from flask import Blueprint, request, jsonify
from app.dto.post_dto import PostRequestDTO, GeoPoseRequestDTO
from app.repositories.post_repository import PostRepository
from app.services.post_service import PostService

class PostController:
    def __init__(self):
        self.bp = Blueprint("post_bp", __name__)
        self.repository = PostRepository()
        self.service = PostService(self.repository)
        self.register_routes()
        
    def register_routes(self):
        @self.bp.route("/", methods=["GET"])
        def get_posts():
            return jsonify(self.service.get_all_posts())

        @self.bp.route("/five", methods=["GET"])
        def get_five_posts():
            data = request.get_json()
            dto = GeoPoseRequestDTO(
                latitude=data["latitude"],
                longitude=data["longitude"],
                altitude=data["altitude"]
            )
            return jsonify(self.service.get_five_posts_in_condition(dto))
        
        @self.bp.route("/", methods=["POST"])
        def add_post():
            data = request.get_json()
            dto = PostRequestDTO(
                latitude=data["latitude"],
                longitude=data["longitude"],
                altitude=data["altitude"],
                x=data["x"],
                y=data["y"],
                z=data["z"],
                w=data["w"],
                image_base64=data["image_base64"]
            )
            if not dto.image_base64:
                return jsonify({"error": "No image data"}), 400
            try:
                post = self.service.create_post(dto)
                return jsonify(post), 201
            except Exception as e:
                return jsonify({"error": str(e)}), 500