# meARy: AR 기술을 활용한 소셜 네트워킹 서비스

---

### 1. 프로젝트 배경
#### 1.1. 국내외 시장 현황 및 문제점
21세기 정보통신 기술의 발전과 스마트폰의 보편화는 소셜 네트워킹 서비스(SNS)의 폭발적인 성장을 이끌었습니다.  
페이스북, 인스타그램 등은 핵심적인 소통 채널로 자리 잡았으나, 여전히 텍스트, 사진, 동영상 등 2차원 미디어 중심으로만 제공되어 사용자의 경험을 제한합니다.  

#### 1.2. 필요성과 기대효과
본 프로젝트는 이러한 한계를 넘어, 기존 SNS 경험을 3차원 공간으로 확장하여 새로운 소셜 네트워킹 경험을 제공합니다.  
- 사진 한 장으로 사용자의 3D 모델 생성 및 AR 배치 가능
- 기록을 단순히 보는 것이 아니라, 직접 방문·상호작용하며 공유 가능 
- 몰입감과 유대감 증진, 새로운 추억 공유 방식 제시  

---

### 2. 개발 목표
#### 2.1. 목표 및 세부 내용
최종 목표:  
사용자가 생성한 영상 콘텐츠를 기반으로 사용자를 닮은 3D Model을 생성하고,  
이를 AR 환경에 재현하여 몰입적 상호작용이 가능한 소셜 네트워킹 서비스를 개발하는 것.  

주요 기능:  
- **AR 포스팅 업로드**: 사진 촬영 → 인물 인식 → 좌표 추출 → 3D 모델 포즈 리타겟팅 → 서버 저장  
- **AR 포스팅 조회**: 특정 위치 방문 → Flask 서버 요청 → 3D 모델 로드 및 AR Anchor 배치  

#### 2.2. 기존 서비스 대비 차별성
- 기존 서비스: 2D 기반 콘텐츠 소비  
- 본 프로젝트:  
  - 사진 한 장으로 개인화된 3D 모델 생성  
  - 포즈 리타겟팅 및 지리적 좌표 기반 배치 

#### 2.3. 사회적 가치 도입 계획
- 디지털 기록을 물리적 공간에 남기는 새로운 소통 패러다임
- 장소에 의미를 부여하고, 사용자 간 유대감 증진  
- 디지털 상호작용을 현실로 확장 → 지속 가능한 소셜 경험 창출  

---

### 3. 시스템 설계
#### 3.1. 시스템 구성도
- **클라이언트 (Unity + ARCore)**: AR 포스팅 업로드 및 조회  
- **백엔드 (Flask 서버, Ubuntu)**: API 처리 및 DB 관리  
- **3D 파이프라인 (Blender)**: 포즈 리타겟팅 처리  
- **데이터베이스 및 스토리지**: SQLite, 로컬 저장소(GLB)  
- **외부 API**: Google ARCore Geospatial API  

#### 3.2. 사용 기술
- **클라이언트**: Unity, AR Foundation, ARCore Extensions, MediaPipe Unity Plugin (C#)  
- **백엔드**: Flask, SQLite, ngrok  
- **3D 모델링**: tripo3D, Adobe Mixamo, Blender, MediaPipe BlazePose  

---

### 4. 개발 결과
#### 4.1. 전체 시스템 흐름도
- **AR 포스팅 업로드**  
  1. 사진 촬영 및 인물 감지  
  2. 좌표 추출 (ARCore → 지구 좌표 변환)  
  3. 서버 전송 → 포즈 추출 및 3D 모델 리타겟팅  
  4. 결과물(GLB) 저장  

- **AR 포스팅 조회**  
  1. 사용자 위치 확인 (ARCore Geospatial API)  
  2. 서버에서 가까운 포스팅 응답 (GLB, 좌표)  
  3. AR Anchor 위에 모델 배치  

#### 4.2. 기능 설명 및 주요 기능 명세서
- **Human Detection (Unity, MediaPipe Plugin)**: 사진 속 인물 감지  
- **Pose Retargeting (Blender)**: 33개 포즈 랜드마크 → 모델 스켈레톤 정렬  
- **AR 모델 배치 보정**: GPS 고도 오차를 AR Plane Detection 기반으로 보정  

#### 4.3. 디렉토리 구조
meARy/
├── backend/
│ ├── app.py # Flask 서버
│ ├── requirements.txt
│ ├── retargeting/ # Blender 파이프라인 스크립트
│ │ └── main.py
│ ├── database/
│ │ └── meary.db
│ └── storage/
│ └── models/
├── unity_project/
│ ├── Assets/
│ │ ├── Scripts/
│ │ ├── Prefabs/
│ │ └── Plugins/
│ └── ...
└── README.md

#### 4.4. 산업체 멘토링 의견 및 반영 사항
> 멘토 피드백과 적용한 사례 정리

---

### 5. 설치 및 실행 방법
>
#### 5.1. 설치절차 및 실행 방법
> 설치 명령어 및 준비 사항, 실행 명령어, 포트 정보 등
#### 5.2. 오류 발생 시 해결 방법
> 선택 사항, 자주 발생하는 오류 및 해결책 등

### 6. 소개 자료 및 시연 영상
#### 6.1. 프로젝트 소개 자료
- 프로젝트 최종 보고서 (PDF)
- 발표 자료 (PPT)

#### 6.2. 시연 영상
- [시연 영상 링크](https://www.youtube.com/watch?v=6Jr0kbTqL4k)
- 주요 장면: 사진 촬영 화면, 모델 배치 화면

---

### 7. 팀 구성
#### 7.1. 팀원별 소개 및 역할 분담
| 이름   | 역할 |
|--------|-------------------------------|
| 김진영 | Unity AR Foundation, Google ARCore Extensions 기반 AR 기능 및 안드로이드 앱 개발 |
| 임석윤 | Unity AR Foundation, Google ARCore Extensions 기반 AR 기능 개발, 백엔드 개발 |
| 허취원 | 3D 모델 생성 및 Blender 기반 Pose Retargeting Pipeline 개발 |

#### 7.2. 팀원 별 참여 후기
- 김진영: *(작성 예정)*
- 임석윤: *(작성 예정)*
- 허취원: *(작성 예정)*

---

### 8. 참고 문헌 및 출처
1. V. Bazarevsky, I. Grishchenko, K. Raveendran, T. Zhu, F. Zhang, and M. Grundmann,  
   *BlazePose: On-device Real-time Body Pose Tracking,* arXiv preprint arXiv:2006.10204, Jun. 2020.  

2. Google Research. (2020, Aug 13). *On-Device, Real-time Body Tracking with MediaPipe BlazePose.*  
   [https://research.google/blog/on-device-real-time-body-pose-tracking-with-mediapipe-blazepose](https://research.google/blog/on-device-real-time-body-pose-tracking-with-mediapipe-blazepose)  

3. Unity Technologies. *AR Foundation Manual*  
   [https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.1/manual/index.html](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.1/manual/index.html)  

4. Google ARCore. *ARCore Extensions for AR Foundation*  
   [https://developers.google.com/ar/develop](https://developers.google.com/ar/develop)  

