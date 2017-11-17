import os
import csv
import pandas as pd

"""

csv_dir = r'csv/'
merged_dir = r'csv/'

csv_list = os.listdir(csv_dir)
merged_name = merged_dir + 'merged_png.csv'
merged_file = open(merged_name, 'w', newline='')
csv_writer = csv.writer(merged_file)

for csv_name in csv_list:
    current_name = csv_dir + csv_name
    current_file = open(current_name, 'r')
    csv_reader = csv.reader(current_file)

    cnt = 0
    for line in csv_reader:
        csv_writer.writerow(line)

    current_file.close()

merged_file.close()

"""
merged_dir = r'csv/'
merged_name = merged_dir + 'merged_png.csv'
merged_file = open(merged_name, 'r')

cleaned_dir = r'csv/'
cleaned_name = cleaned_dir + 'cleaned_png.csv'
cleaned_file = open(cleaned_name, 'w', newline='')


csv_reader = csv.reader(merged_file)
csv_writer = csv.writer(cleaned_file)
index_line = []
for i in range(0, 200):
    index_line.append(i + 1)

csv_writer.writerow(index_line)

for row in csv_reader:
    csv_writer.writerow(row)
