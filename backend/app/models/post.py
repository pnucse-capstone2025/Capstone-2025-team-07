from app.db import db
from app.models.geopose import GeoPose
from app.models.quaternion import Quaternion

class Post(db.Model):
    __tablename__ = "post"
    id = db.Column(db.BigInteger, primary_key=True)
    latitude = db.Column(db.Float)
    longitude = db.Column(db.Float)
    altitude = db.Column(db.Float)
    x = db.Column(db.Float)
    y = db.Column(db.Float)
    z = db.Column(db.Float)
    w = db.Column(db.Float)

    @property
    def geopose(self) -> GeoPose:
        return GeoPose(self.latitude, self.longitude, self.altitude)
    
    @property
    def quaternion(self) -> Quaternion:
        return Quaternion(self.x, self.y, self.z, self.w)

    @geopose.setter
    def geopose(self, gp: GeoPose):
        self.latitude = gp.latitude
        self.longitude = gp.longitude
        self.altitude = gp.altitude

    @quaternion.setter
    def quaternion(self, q: Quaternion):
        self.x = q.x
        self.y = q.y
        self.z = q.z
        self.w = q.w