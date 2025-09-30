import os
import base64
from app.dto.post_dto import PostRequestDTO, PostResponseDTO, PostsResponseDTO, GeoPoseRequestDTO
from app.models.post import Post
from app.repositories.post_repository import PostRepository
from app.services.blender.pipeline import run_pipeline


class PostService:
    def __init__(self, repository: PostRepository):
        BASE_DIR = os.path.abspath(os.path.dirname(os.path.dirname(os.path.dirname(__file__))))
        self.image_folder = os.path.join(BASE_DIR, "images")
        self.pose_folder = os.path.join(BASE_DIR, "poses")
        self.prefab_folder = os.path.join(BASE_DIR, "prefabs")
        self.repository = repository

    def get_all_posts(self):
        posts = self.repository.get_all_posts()
        return [PostResponseDTO(post).to_dict() for post in posts]

    def get_five_posts_in_condition(self, dto: GeoPoseRequestDTO):
        posts = self.repository.get_five_posts_by_condition(dto.geopose)
        posts = [PostResponseDTO(post).to_dict() for post in posts]
        return PostsResponseDTO(posts).to_dict()

    def create_post(self, dto: PostRequestDTO):
        try:
            new_post = Post()
            new_post.geopose = dto.geopose
            new_post.quaternion = dto.quaternion
            self.repository.add(new_post)
            img_data = base64.b64decode(dto.image_base64)
            img_name = f"{new_post.id}.jpg"
            save_path = os.path.join(self.image_folder, img_name)
            with open(save_path, "wb") as f:
                f.write(img_data)
            self.repository.commit()
            run_pipeline(new_post.id)
            return PostResponseDTO(new_post).to_dict()
        
        except Exception as e:
            self.repository.rollback()
            if save_path and os.path.exists(save_path):
                os.remove(save_path)
            prefab_path = os.path.join(self.prefab_folder, f"{new_post.id}.glb") if new_post else None
            if prefab_path and os.path.exists(prefab_path):
                os.remove(prefab_path)
            return e
        
        finally:
            pose_path = os.path.join(self.pose_folder, f"{new_post.id}.json") if new_post else None
            if pose_path and os.path.exists(pose_path):
                os.remove(pose_path)
