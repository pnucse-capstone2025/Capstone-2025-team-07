import os
from flask import Flask
from .db import db

def create_app():
    app = Flask(__name__)

    BASE_DIR = os.path.abspath(os.path.dirname(os.path.dirname(__file__)))
    db_path = os.path.join(BASE_DIR, "database.db")
    app.config['SQLALCHEMY_DATABASE_URI'] = f"sqlite:///{db_path}"
    app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False

    db.init_app(app)

    from . import models

    from .controllers.post_controller import PostController
    post_controller = PostController()
    app.register_blueprint(post_controller.bp, url_prefix="/posts")

    from .controllers.file_controller import FileController
    file_controller = FileController()
    app.register_blueprint(file_controller.bp, url_prefix="/download")

    return app