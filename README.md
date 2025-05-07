# 정시퇴근(진짜) 화재안전 프로젝트

## 자세한 사항은 지라 & 컨플루언스 에서~
* 지라 초기 파지면 생성 예정
---
# 프로젝트 초기 세팅
## 기본 - 윈도우의 터미널 (cmd 실행, 깃 폴더 있는 위치까지 이동)
## 1. 깃훅 추가 (커밋 메시지 맞추기)
> git config core.hooksPath .gitsettings
* 컨벤셔널 커밋 강제하기 위한 세팅
* 로컬의 깃 훅을 이용해서 커밋 메시지 제한을 한다. (실수 방지) - [깃훅 참고자료](https://fobidlim.medium.com/git-hooks-lint%EB%A1%9C-%EC%BD%94%EB%93%9C%EC%8A%A4%ED%83%80%EC%9D%BC-%ED%86%B5%EC%9D%BC-a2577f609cfb)
* 쓸수 있는 타입의 종류, 우리 프로젝트에서는 아래 정도만 쓸것같다.
```csharp
# 타입 종류
# feat : 새로운 기능 추가
# fix : 버그 수정
# docs : 문서 수정
# style : 코드 의미에 영향을 주지 않는 변경사항
# refactor : 코드 리팩토링
# test : 테스트 코드 추가
# chore : 빌드 부분 혹은 패키지 매니저 수정사항 등등
```
### 커밋 메시지 예시 (평상시 습관 들였으면 똑같음)
> feat: 캐릭터 기능 업데이트
> - 띵패밀리 캐릭터 수정
> - 애니메이션 추가

> fix(오브젝트): 소화기 버그 수정
> - 분사되는 시간 수정

등등등 깃 커밋 메시지는 맨 윗줄만 체크함.

### 컨벤셔널 커밋이란?
- https://www.conventionalcommits.org/ko/v1.0.0/

---
## 2. 개행문자 수정
* [깃 개행문자 설정 참고자료](https://dsaint31.tistory.com/209)
> ### 윈도 쓰는사람 설정
> git config --global core.autocrlf true
> ### 맥 쓰는 사람 설정
> git config --global core.autocrlf input

---
## 3. .editorconfig 파일 설정
### .editorconfig 파일이란?
* 환경이 다르더라도 동일한 환경에서 협업 할 수 있도록 에디터의 환경을 맞춰주는 설정파일
* 추가적으로 궁금하다면 링크 참고
  * [마소 공식문서](https://learn.microsoft.com/ko-kr/visualstudio/ide/create-portable-custom-editor-options?view=vs-2022)
  * [위 문서 실제 파일로 만든거](https://github.com/pankaxz/UnityEditorConfig)
* [Visual Studio 설정 방법](https://rito15.github.io/posts/unity-editorconfig-encoding/)
* Rider 설정 방법 - https://www.jetbrains.com/help/rider/Using_EditorConfig.html#in_solution
---
## 4. 캐릭터 패키지 풀기 (기업 제공)
* 01_Characters의 유니티 패키지 풀기
* 모델 100MB 넘는거 있어서 깃에 푸시 안되므로, 풀고 그 폴더에 그대로 냅두기 (_Models 폴더 커밋 안되게 설정 해놓음)

---
# 프로젝트 사용 플러그인
1. URP - 쉐이더 그래프 활용
2. Pun2 - 멀티플레이어 활용
3. XR (VR) - 오큘러스 리프트 개발
4. ToonShader - 캐릭터 툰쉐이딩 적용
5. TimeLine - 컷신에 사용

등등 추가 필요하면 더 추가 예정~

---
# 깃 전략 설명
* 간략화된 깃 플로우 전략
* 근데 이제 QA 브랜치 추가 핧 거임..
https://dduckchul.github.io/2025-03-12-NetworkGameProject/

---
# 대락적인 설계
![러프버전](러프버전.png)

---

# 프로젝트 완료 문서
## 클래스 다이어그램
* 프로젝트 완료시 추가 예정


