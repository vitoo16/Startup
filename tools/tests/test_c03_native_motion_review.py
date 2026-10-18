"""Fast synthetic checks for C03 motion preflight after owner-rejected Idle v1."""
import importlib.util
import json
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
SCRIPT=ROOT/'scripts/Review-C03NativeMotion.py'
SPEC=ROOT/'docs/evidence/production-art/source/c03-authoring/C03-idle-v2-art-direction.json'
module_spec=importlib.util.spec_from_file_location('c03_motion_review',SCRIPT)
review=importlib.util.module_from_spec(module_spec)
module_spec.loader.exec_module(review)


def put(image,x,y,color):
    start=(y*128+x)*4
    image[start:start+4]=bytes((*color,255))


def fixtures():
    contract=json.loads(SPEC.read_text(encoding='utf8'))
    first=bytearray(128*160*4)
    # Preserve foreground so that sample has a planted foot and ordinary native sprite.
    for y in range(10,154):
        for x in range(35,96):
            if 75<y<130:
                put(first,x,y,(92,134,103))
    for y in range(140,152):
        for x in list(range(35,55))+list(range(75,95)):
            put(first,x,y,(58,58,65))
    first=bytes(first)
    frames=[first]*6
    return contract,frames


class C03NativeMotionReviewTests(unittest.TestCase):
    def test_head_stable_microexpression_with_holds_is_allowed_to_preflight_only(self):
        contract,frames=fixtures()
        variant=bytearray(frames[0])
        for x in range(50,62):
            put(variant,x,56,(75,57,60))
        for x in range(57,69):
            put(variant,x,91,(73,110,84))
        frames[2]=bytes(variant)
        frames[3]=bytes(variant)  # deliberate rest duration is NOT a failed animation
        report=review.inspect(frames,contract)
        self.assertEqual(report['status'],'STRUCTURAL_PREFLIGHT_ONLY_OWNER_PENDING')
        self.assertGreater(report['held_adjacent_pairs'],0)
        self.assertFalse(report['passed_artistic_review'])
        self.assertEqual(report['occlusion_layer_proof'],'PENDING_AUTHORED_PER_KEYPOSE_LAYER_MASKS')

    def test_rejects_legacy_style_excessive_head_wobble(self):
        contract,frames=fixtures()
        change=bytearray(frames[0])
        for y in range(22,27):
            for x in range(45,90):
                put(change,x,y,(70,52,51))
        for x in range(50,62):
            put(change,x,90,(65,110,83))
        frames[2]=bytes(change)
        report=review.inspect(frames,contract)
        self.assertIn('EXCESS_HEAD_PIXEL_CRAWL',report['issues'])
        self.assertIn('HEAD_MOVES_OUT_OF_PROPORTION_TO_TORSO',report['issues'])
        self.assertEqual(report['status'],'REJECTED_MOTION_PREFLIGHT')

    def test_rejects_head_dominance_even_below_absolute_cap(self):
        contract,frames=fixtures()
        change=bytearray(frames[0])
        for y in range(21,26):
            for x in range(45,65):
                put(change,x,y,(70,52,51))
        for x in range(51,61):
            put(change,x,90,(65,110,83))
        frames[2]=bytes(change)
        report=review.inspect(frames,contract)
        self.assertIn('HEAD_MOVES_OUT_OF_PROPORTION_TO_TORSO',report['issues'])
        self.assertNotIn('EXCESS_HEAD_PIXEL_CRAWL',report['issues'])

    def test_rejects_moving_planted_shoes(self):
        contract,frames=fixtures()
        change=bytearray(frames[0])
        put(change,49,148,(250,250,240))
        frames[3]=bytes(change)
        report=review.inspect(frames,contract)
        self.assertIn('IDLE_PLANTED_FEET_NOT_STABLE',report['issues'])

    def test_rejects_no_motion_and_edge_clipping(self):
        contract,frames=fixtures()
        report=review.inspect(frames,contract)
        self.assertIn('NO_AUTHORED_IDLE_MOTION',report['issues'])
        change=bytearray(frames[0])
        put(change,0,35,(255,0,0))
        frames[1]=bytes(change)
        report=review.inspect(frames,contract)
        self.assertIn('SPRITE_TOUCHES_CANVAS_EDGE',report['issues'])

    def test_rejects_approval_conflation_and_nonbinary_alpha(self):
        contract,frames=fixtures()
        contract['owner_approved_idle_animation']=True
        with self.assertRaisesRegex(ValueError,'approval'):
            review.inspect(frames,contract)
        contract,frames=fixtures()
        change=bytearray(frames[0])
        change[(50*128+50)*4+3]=128
        frames[1]=bytes(change)
        with self.assertRaisesRegex(ValueError,'Nonbinary'):
            review.inspect(frames,contract)


if __name__=='__main__':
    unittest.main()
