# CamAI: V2 Migration & Optimization Report
**Date:** 2026-04-27
**Status:** Completed & Optimized

## 1. Overview
The CamAI system has been successfully migrated from a static 3-angle face registration (V1) to a dynamic, continuous multi-angle enrollment pipeline (V2). This upgrade significantly improves recognition accuracy by allowing the AI to learn a subject's face from a wide variety of perspectives (Yaw angles).

## 2. Key Technical Improvements

### 2.1. Continuous Enrollment Pipeline (V2)
- **Old (V1):** Manual capture of "Front", "Left", and "Right" images via the UI.
- **New (V2):** Interactive polling-based enrollment.
    - AI Engine automatically tracks and captures multiple face angles based on Yaw landmarks.
    - Captures at least 5-10 distinct angles (e.g., -45°, -30°, 0°, +30°, +45°).
    - Frontend provides a real-time progress bar based on the enrollment status from the API.

### 2.2. Database Schema Refactoring
- **Dropped Tables:** `UserFaces` (Legacy V1 storage).
- **New Storage Model:** `FaceEmbeddings` table now stores an arbitrary number of embeddings per profile.
- **Stored Procedures:** Migrated all data access logic to specific V2 procedures (`sp_FaceProfile_RegisterV2`, `sp_FaceEmbedding_Add`, `sp_FaceEmbedding_GetAll`).
- **Data Integrity:** Implemented CASCADE deletes to ensure that deleting a profile automatically cleans up all associated multi-angle embeddings.

### 2.3. Dual-Stream View Logic
- **Annotated Stream (`/api/FaceStream/live`):** Displays detection boxes, labels, and similarity scores. Reserved for system verification and debugging.
- **Clean Stream (`/api/FaceStream/live-clean`):** Provides a clear, professional video feed without AI overlays. Used as the default primary view on the Web Dashboard.

### 2.4. Performance & Recognition Quality
- **EnhanceImageQuality:** Integrated a real-time pre-processing pipeline in `CameraService` (Unsharp Masking + CLAHE) to improve recognition in low-light or backlit scenarios.
- **MAX Similarity Matching:** The AI Matcher now compares an input face against *all* stored angles for a profile and returns the highest similarity score, significantly reducing False Rejections (FRR).

## 3. Deployment & Infrastructure
- **MinIO Storage:** Unified bucket hierarchy `faces/register` and `faces/logs`. Access is managed via Secure Presigned URLs with 1-hour expiry.
- **Hybrid Support:** The backend API includes a service-layer bridge to allow legacy V1 registration requests to be transparently stored into the V2 database structure.

## 4. Next Steps
- **Vector Database Integration:** As the face database grows, a migration to a vector-optimized database (e.g., Milvus/Qdrant) is recommended for O(log N) search performance.
- **Active Anti-Spoofing:** Future updates could include 3D depth or texture analysis to prevent spoofing from high-resolution photos/screens.
