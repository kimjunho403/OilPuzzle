# 생성 및 출처 기록

작성: 2026-09-29. 이미지 생성 방식: built-in `image_gen`.

| 결과 | 최종 파일 | 프롬프트 |
|---|---|---|
| 예상 화면 | `Docs/Art/Concepts/Mala-Target-v01.png` | `01-target-screen.prompt.txt` |
| 식탁 | `Assets/Oilpuz/Art/Mala/Table.png` | `02-Table.prompt.txt` |
| 그릇과 국물 | `Assets/Oilpuz/Art/Mala/BowlBroth.png` | `03-BowlBroth.prompt.txt` |
| 표고버섯 | `Assets/Oilpuz/Art/Mala/Shiitake.png` | `04-Shiitake.prompt.txt` |
| 완자 | `Assets/Oilpuz/Art/Mala/FishBall.png` | `05-FishBall.prompt.txt` |
| 청경채 | `Assets/Oilpuz/Art/Mala/BokChoy.png` | `06-BokChoy.prompt.txt` |

프로젝트 내 원본 PNG: 식탁 941×1672 RGB, 나머지 1254×1254 RGBA. 투명 스프라이트의 모서리 알파=0 확인. Unity에서만 텍스처 크기와 압축을 조정하며 원본을 덮어쓰지 않음.

기름과 UI는 코드로 생성되는 동적 그래픽이다. `Art/Generated/PreviewDensity.exr`은 에디터에서 씬을 미리 보여주기 위한 밀도장 캐시이며 생성형 이미지가 아니다. Play에서는 실시간 입자 밀도장으로 대체한다.

## 폰트

- 폰트: Noto Sans CJK KR Regular.
- 출처: https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/Korean
- 원본: https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/OTF/Korean/NotoSansCJKkr-Regular.otf
- 라이선스: https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE
- 라이선스 원문: `Assets/Oilpuz/Fonts/OFL.txt`.

운영체제의 맑은 고딕은 복사하거나 배포하지 않았다.
