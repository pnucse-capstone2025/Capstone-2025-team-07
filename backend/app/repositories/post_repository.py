import random
from app.db import db
from app.models.post import Post

class PostRepository:
    def get_all_posts(self):
        return Post.query.all()

    def get_five_posts_by_condition(self, geopose):
        lst_in_condition = [post for post in Post.query.all()
                            if post.geopose.is_in_200m_cube(geopose)]
        if len(lst_in_condition) < 5:
            return lst_in_condition
        return random.sample(lst_in_condition, 5)

    def add(self, post: Post):
        db.session.add(post)
        db.session.flush()
        return post
    
    def commit(self):
        db.session.commit()

    def rollback(self):
        db.session.rollback()