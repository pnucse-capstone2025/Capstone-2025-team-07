class GeoPose:
    def __init__(self, latitude: float, longitude: float, altitude:float):
        self.latitude = latitude
        self.longitude = longitude
        self.altitude = altitude

    def to_dict(self):
        return {
            "latitude": self.latitude,
            "longitude": self.longitude,
            "altitude": self.altitude
        }

    def is_in_200m_cube(self, other: "GeoPose"):
        range = 0.000898
        return (abs(self.latitude - other.latitude) <= range and
                abs(self.longitude - other.longitude) <= range)
    