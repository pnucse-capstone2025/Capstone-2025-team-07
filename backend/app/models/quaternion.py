class Quaternion:
    def __init__(self, x: float, y: float, z: float, w: float):
        self.x = x
        self.y = y
        self.z = z
        self.w = w

    def to_dict(self):
        return {
            "x": self.x,
            "y": self.y,
            "z": self.z,
            "w": self.w
        }