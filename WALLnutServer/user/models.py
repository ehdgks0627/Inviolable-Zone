from django.db import models
from api_key.models import APIkey

# Create your models here.

class User(models.Model):
    access_token = models.CharField(max_length=32, primary_key=True)
    name = models.CharField(max_length=128, blank=True)
    api_key = models.ForeignKey(to=APIkey, on_delete=models.SET_NULL, blank=False, null=True)
    create_time = models.DateField(blank=False, null=False, auto_now_add=True)
    last_access_time = models.DateTimeField(blank=False, null=False, auto_now=True)

    def isPaid(self):
        return self.api_key.isPaid()

    def __str__(self):
        return "<{}, {}>".format(self.name, self.isPaid())

    def __unicode__(self):
        return "<{}, {}>".format(self.name, self.isPaid())

    def __repr__(self):
        return "<{}, {}>".format(self.name, self.isPaid())
