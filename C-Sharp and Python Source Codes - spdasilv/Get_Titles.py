# Stefanno Da Silva - 20508389
# 3A Management Engineering
# March 21st, 2016
#
# This simple program reads and searches the
# titles of all documents contained within
# the 'topics.401-450.txt' file.

file = open('topics.401-450.txt', 'r')
titles = []
line_arr = []

# Read each line in search for titles. Store
# all titles into a list.
for line in file:
    line_arr = line.split(' ')
    if line_arr[0] == '<title>':
        curr_title = line
        titles.append(curr_title.replace('<title>', '', 1))

# Write to a file each topic number and
# respective query.
text_file = open('Queries.txt','w')
topic = 401
while (topic < 451):
    text_file.write('Topic %s:%s' % (topic, titles[topic-401]))
    topic = topic + 1
text_file.close()
