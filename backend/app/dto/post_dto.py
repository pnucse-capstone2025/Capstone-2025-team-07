from app.models.geopose import GeoPose
from app.models.quaternion import Quaternion

class PostRequestDTO:
    def __init__(self, latitude: float, longitude: float, altitude: float, x: float, y: float, z: float, w: float, image_base64):
        self.geopose = GeoPose(latitude, longitude, altitude)
        self.quaternion = Quaternion(x, y, z, w)
        self.image_base64 = image_base64

class PostResponseDTO:
    def __init__(self, post):
        self.id = post.id
        self.geopose = post.geopose
        self.quaternion = post.quaternion
        

    def to_dict(self):
        return {
            "id": self.id,
            "geopose": self.geopose.to_dict(),
            "quaternion": self.quaternion.to_dict()
        }
    
class PostsResponseDTO:
    def __init__(self, posts):
        self.posts = posts

    def to_dict(self):
        return {
            "posts": self.posts
        }
    
class GeoPoseRequestDTO:
    def __init__(self, latitude: float, longitude:float, altitude: float):
        self.geopose = GeoPose(latitude, longitude, altitude)