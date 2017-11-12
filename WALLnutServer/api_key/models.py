from django.db import models


# Create your models here.

class APIkey(models.Model):
    serial = models.CharField(max_length=32, primary_key=True)
    paid = models.BooleanField(blank=False, null=False)
    used = models.BooleanField(blank=False, null=False, default=False)

    def isPaid(self):
        return self.paid

    def __str__(self):
        return "<{}, {}>".format(self.serial, self.paid)

    def __unicode__(self):
        return "<{}, {}>".format(self.serial, self.paid)

    def __repr__(self):
        return "<{}, {}>".format(self.serial, self.paid)
