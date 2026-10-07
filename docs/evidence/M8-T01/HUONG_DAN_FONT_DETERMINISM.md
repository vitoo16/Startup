# Hướng dẫn kiểm tra tính ổn định của font TMP cho M8-T01

Tài liệu này mô tả đúng quy trình đã được kiểm chứng với Unity `6000.3.25f1`, revision `e1dba0a9aba4`. Mục tiêu là chứng minh rằng hai font TMP đã được Unity 6.3 chuẩn hóa một lần và không tiếp tục làm bẩn Git sau một lần import hoàn toàn mới.

## Kết quả đã xác nhận

- `NotoSansVietnamese.asset` vẫn là font Dynamic. Không chuyển sang Static và không tạo lại atlas.
- Bảng glyph và character được lưu trong asset vẫn là `0 / 0`; atlas vẫn là một texture rỗng 1×1.
- Unity chuẩn hóa bảng OpenType của Noto một lần. Lần import mới tiếp theo không thay đổi asset.
- `LiberationSans SDF - Fallback.asset` được Unity nâng serialization của Material từ phiên bản `6` lên `8`. Lần import mới tiếp theo không thay đổi asset.
- Lần chạy cuối: EditMode `8/8`, PlayMode `11/11`, runner exit `0`, C# error/warning `0/0`, `git status --short` trống và `git diff --check` trả về `0`.

## Chuẩn bị worktree an toàn

Không dùng checkout chính nếu checkout đó đang có thay đổi của bạn. Không chạy `checkout`, `reset`, `clean` hoặc xóa file trên checkout đang làm việc.

Để review hoặc chạy lại trạng thái đã chuẩn hóa, dùng worktree riêng của branch hiện có `fix/m8-t01-font-determinism` và xác nhận HEAD khớp commit đang được review. Branch này đã đi qua bước chuẩn hóa nên HEAD của nó không còn là SHA main ban đầu.

Để tái hiện Pass A từ đầu, tạo một worktree thí nghiệm riêng ở đúng SHA main ban đầu rồi dùng một tên branch thí nghiệm khác:

```powershell
$Repo = 'C:\Users\viett\Startup'
$Worktree = 'C:\duong-dan-rieng\m8-t01-font-determinism'
$StartSha = 'ce7bb299f120f6fc9ebae78413d8764c2c8057ce'

git -C $Repo worktree add --detach $Worktree $StartSha
git -C $Worktree switch -c experiment/m8-t01-font-determinism
git -C $Worktree rev-parse HEAD
git -C $Worktree branch --show-current
git -C $Worktree status --short
```

Ba kết quả bắt buộc của thí nghiệm là đúng SHA ban đầu, đúng branch thí nghiệm và `git status --short` không in gì. Không tạo lại branch closeout đang tồn tại và không ghi đè worktree khác.

Kiểm tra pin của project:

```powershell
Get-Content "$Worktree\ProjectSettings\ProjectVersion.txt"
```

Giá trị phải là Unity `6000.3.25f1 (e1dba0a9aba4)`. Log của mỗi lần chạy cũng phải có cả version và revision này. Không mở hoặc lưu project bằng một bản Unity khác.

## Chạy kiểm tra chuẩn

Luôn chạy script chuẩn từ worktree riêng:

```powershell
Set-Location $Worktree
./scripts/Test-UnityHeadless.ps1 `
  -Mode All `
  -OutputDirectory 'C:\duong-dan-bang-chung\PASS-A'
```

Sau khi runner kết thúc, kiểm tra:

```powershell
git status --short
git diff --check
git diff -- `
  'Assets/StartupLife/UI/Fonts/NotoSansVietnamese.asset' `
  'Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset'
```

Ở Pass A từ main cũ, chỉ hai font trên được phép thay đổi. Phải ghi lại số glyph, character, atlas và các bảng feature trước khi giữ candidate do Unity tạo ra. Không gọi thay đổi lớn của bảng feature là “chỉ đổi serialization”.

Giữ bản vá Pass A bên ngoài repository trước khi commit candidate:

```powershell
$EvidenceRoot = 'C:\duong-dan-bang-chung'
New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
$PassAPatch = Join-Path $EvidenceRoot 'PASS-A-font-normalization.patch'

git diff --binary --output=$PassAPatch -- `
  'Assets/StartupLife/UI/Fonts/NotoSansVietnamese.asset' `
  'Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset'

Get-Item -LiteralPath $PassAPatch
```

Nếu diff đã được xác nhận chỉ có đúng hai font canonical do Unity tạo ra, commit chính xác hai đường dẫn đó. Không dùng `git add -A`:

```powershell
git add -- `
  'Assets/StartupLife/UI/Fonts/NotoSansVietnamese.asset' `
  'Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset'

git diff --cached --name-only
git commit -m 'fix: normalize TMP font assets for Unity 6.3'
git status --short
```

Trước khi bắt đầu Pass B, `git status --short` phải trống. Nếu candidate chưa được commit thì Pass B vẫn sẽ so với main cũ và không thể chứng minh trạng thái đã chuẩn hóa là sạch.

## Tạo fresh import mà không xóa nhầm dữ liệu

Trước Pass B, đóng Unity Editor. Không dùng lệnh xóa đệ quy rộng. Quy trình đã kiểm chứng di chuyển duy nhất cache `Library` của worktree riêng sang một thư mục backup có đường dẫn tuyệt đối đã xác nhận:

```powershell
$EvidenceRoot = 'C:\duong-dan-bang-chung'
$EditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe'
New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
$Library = [IO.Path]::GetFullPath((Join-Path $Worktree 'Library'))
$Expected = [IO.Path]::GetFullPath("$Worktree\Library")
$Backup = [IO.Path]::GetFullPath((Join-Path $EvidenceRoot 'Library-PASS-A-backup'))

if ($Library -ne $Expected) { throw 'Sai đường dẫn Library' }
if (-not $Library.StartsWith(
    [IO.Path]::GetFullPath($Worktree) + [IO.Path]::DirectorySeparatorChar,
    [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Library nằm ngoài worktree riêng'
}
$RunningEditor = Get-Process Unity -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $EditorPath }
if ($RunningEditor) {
    throw 'Unity Editor 6000.3.25f1 vẫn đang chạy'
}
if (-not (Test-Path -LiteralPath $Library -PathType Container)) {
    throw 'Không tìm thấy Library cần di chuyển'
}
if (Test-Path -LiteralPath $Backup) {
    throw 'Đường dẫn backup đã tồn tại'
}

Move-Item -LiteralPath $Library -Destination $Backup
```

Kiểm tra trên chỉ xét executable Editor chính xác và không đọc hoặc in command line/token. Một tiến trình broker của Unity CLI không phải Editor có thể vẫn tồn tại và không cần chặn việc di chuyển cache. Lệnh này không đụng vào checkout chính và có thể hoàn tác bằng cách di chuyển backup trở lại khi Unity Editor đang tắt. Sau đó chạy lại script với thư mục bằng chứng mới:

```powershell
Set-Location $Worktree
./scripts/Test-UnityHeadless.ps1 `
  -Mode All `
  -OutputDirectory 'C:\duong-dan-bang-chung\PASS-B'

git status --short
git diff --check
```

Tiêu chí đạt sau Pass B và lần chạy cuối:

- log có marker fresh import và đúng Unity revision;
- compile thành công, C# error/warning `0/0`;
- EditMode `8/8`, PlayMode `11/11`;
- runner exit `0`;
- `git status --short` không in gì;
- `git diff --check` trả về `0`.

## Những việc không được làm

- Không regenerate baseline hoặc tự sửa kết quả test để làm sạch Git.
- Không gọi `Clear Dynamic Data`, không xóa bảng dynamic của font và không re-save font bằng tay.
- Không mở Font Asset Creator để tạo lại Noto hoặc fallback trong closeout này.
- Không chuyển Noto sang Static nếu chưa chứng minh đầy đủ mọi chuỗi UI/localization và bộ dấu tiếng Việt bắt buộc.
- Không sửa YAML để đoán serialization của Unity.
- Không trim sáu dấu cách cuối dòng trong fallback. Unity 6.3 sẽ tạo lại chúng ở lần fresh import tiếp theo.
- Không thêm `.gitattributes` hoặc Git config để che thay đổi.
- Không chạy `git clean`, `git reset --hard` hoặc lệnh xóa đệ quy trên checkout chính.

Sáu dấu cách nói trên là byte canonical do Unity tạo ra. Vì chúng đã được commit, kiểm tra sau runtime trên worktree sạch vẫn đạt: `git status` trống và `git diff --check` trả về `0`. Một phép so sánh cả range với main cũ có thể báo sáu dòng này; đó không phải mutation mới sau runtime.

## Bạn cần làm gì tiếp theo

1. Mở PR `fix/m8-t01-font-determinism` và xác nhận diff chỉ gồm hai font TMP đã chuẩn hóa, bằng chứng và tài liệu closeout M8-T01.
2. Xác nhận PR ghi đúng runtime-tested head, Unity `6000.3.25f1 (e1dba0a9aba4)`, EditMode `8/8`, PlayMode `11/11` và post-run worktree sạch.
3. Không yêu cầu đổi font sang Static và không re-save hai font khi review.
4. Chờ CI của commit tài liệu cuối cùng xanh.
5. Chỉ merge khi chủ sở hữu dự án quyết định rõ ràng. Agent không tự merge.
6. Chỉ bắt đầu M8-T02 sau khi PR closeout đã merge vào main.

Nếu bất kỳ lần chạy lại nào làm thay đổi Noto hoặc fallback sau khi bắt đầu từ canonical head, dừng lại, giữ nguyên diff và log, rồi báo blocker. Không commit lặp lại dữ liệu atlas/font feature đang thay đổi.
