def ideal(count):
        idcg = 0
        for x in range(0, count):
            idcg += (1/math.log(x+2, 2))
        return idcg
import math
# Stefanno Da Silva - 20508389
# 3A Management Engineering
# March 21st, 2016
#
# This program analyses the output of the
# BM25 search engine. It produces a table
# containing the precision@1000 and nDCG@1000
# scores for each query.

# This method calculate the ideal DCG
# score for the top 1000 results retrieved.

# The program starts by creating a
# dictionary containing all the topic
# numbers as keys, and list of all the
# documents and their scores for each
# topic.
file = open('Rankings - spdasilv.txt', 'r')
doc_rankings = {}
for line in file :
    curr_line = line
    line_array = curr_line.split(' ')
    if doc_rankings.__contains__(line_array[0]):
        doc_rankings[line_array[0]].append([line_array[2], line_array[4]])
    else:
        doc_rankings[line_array[0]] = []
        doc_rankings[line_array[0]].append([line_array[2], line_array[4]])
file.close()

# The program opens the qrels file.
# It reads each line and add to a
# dictionary the topic as a key, and
# as value a list containing only the
# relevant documents (judgment = 1).
file = open('LA-only.trec8-401.450.minus416-423-437-444-447.txt', 'r')
doc_qrels = {}
for line in file:
    curr_line = line
    line_array = curr_line.split(' ')
    if doc_qrels.__contains__(line_array[0]) and line_array[3] == '1\n':
        doc_qrels[line_array[0]].append(line_array[2])
    elif line_array[3] == '1\n':
        doc_qrels[line_array[0]] = []
        doc_qrels[line_array[0]].append(line_array[2])
file.close()

# The document iterates each key of the
# dictionary containing the topic ranking
# results. For each topic it checks starting
# form the first position the the 10th if
# document at position i is also contained
# within the dictionary of relevant documents.
#
# For each topic it calculates precision@10 and
# nDCG@10, and outputs it to a .txt file.
text_file = open('Precision.txt','w')
for key in doc_rankings:
    DCG = 0
    count = 0
    length = min(len(doc_rankings[key]),1000)
    for x in range(0, length):
        if doc_rankings[key][x][0] in doc_qrels[key]:
            DCG += (1/math.log(x+2,2))
            count += 1
    precision = count/length
    iDCG = ideal(len(doc_qrels[key]))
    NDCG = 0
    if count > 0:
        NDCG = DCG/iDCG
    text_file.write('Topic %s Precision: %s NDCG(1000): %s \n' % (key, precision,NDCG))
print('Success')
text_file.close()
