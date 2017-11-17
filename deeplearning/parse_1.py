import os
import csv

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


