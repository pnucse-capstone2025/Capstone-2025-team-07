from flask import Blueprint, send_file, abort
import os

class FileController:
    def __init__(self):
        self.bp = Blueprint("file_bp", __name__)
        BASE_DIR = os.path.abspath(os.path.dirname(os.path.dirname(os.path.dirname(__file__))))
        self.image_dir = os.path.join(BASE_DIR, "images")
        self.prefab_dir = os.path.join(BASE_DIR, "prefabs")
        self.register_routes()


    def register_routes(self):
        @self.bp.route("/image/<int:id>", methods=["GET"])
        def download_image(id):
            file_path = os.path.join(self.image_dir, f"{id}.jpg")
            if os.path.exists(file_path):
                return send_file(file_path, mimetype="image/jpeg")
            else:
                abort(404, description=f"Image {id}.jpg not found")
            

        @self.bp.route("/prefab/<id>", methods=["GET"])
        def download_prefab(id):
            file_path = os.path.join(self.prefab_dir, f"{id}.glb")
            if os.path.exists(file_path):
                return send_file(file_path, mimetype="model/gltf-binary")
            else:
                abort(404, description=f"Prefab {id}.glb not found")