import selenium as se
from selenium import webdriver
import time
from lxml import html
from lxml.etree import tostring
import os.path
import numpy as np
import pandas as pd
import re

def pre_scrape(link):

	url = URL_START + link
	browser.get(url)
	innerHTML = "<html><body>"
	innerHTML += browser.execute_script("return document.getElementById(\"col-content\").innerHTML")
	inn = html.fromstring(innerHTML)
	innerHTML += "</body></html>"
	
	
	
	try:
		innerHTML = "<html><body>"
		innerHTML += "<table>"
		# innerHTML += browser.execute_script('return document.getElementById("tournamentTable").firstChild.lastChild.innerHTML')
		
		for i in range(2,7):
			time.sleep(SCROLL_PAUSE_TIME)
			url = URL_START + link + "#/page/" + str(i) + "/"
			browser.get(url)
			innerHTML += browser.execute_script("return document.getElementById(\"tournamentTable\").firstChild.lastChild.innerHTML")
			
		innerHTML += "</table>"
		innerHTML += "</body></html>"
		
		season = html.fromstring(innerHTML)
	except:
		innerHTML += "</table>"
		innerHTML += "</body></html>"
		
		season = html.fromstring(innerHTML)
	return (inn,season)
	
def link_maker(link,year):
	split = link.split("/")
	split[-3] = split[-3] + "-{}".format(year)
	link = "/".join(split)
	
	return link

def scrape(html):
	rows = html.xpath("//tr[contains(@class,\"deactivate\")]")
	for j in range(len(rows)):
		children = rows[j].getchildren()
		match = children[1].text_content()
		final = children[2].text_content()
		odds = [children[n].text_content() for n in range(3,5)]
		teams = match.split(" - ")
		teams = [re.split(" |-",teams[n]) for n in range(2)]
		for n in range(2):
			if (len(teams[n][-2]) == 1) or (teams[n][-2][-1] == "."):
				teams[n][-2] = ""
			if teams[n][-1][-1] == ".":
				ini = teams[n][-1][-2]
				teams[n][-1] = ""
			if teams[n][0] == "Zverev":
				teams[n][1] = "Zverev"
				if ini == "A":
					teams[n][0] = "Alexander"
				else:
					teams[n][0] = "Mischa"
			if (teams[n][0] == "Garcia") & (teams[n][1] == "Lopez"):
				teams[n][0] = "Garcia"
				teams[n][1] = "Lop"
		teams = [" ".join(teams[n]) for n in range(2)]
		teams = [teams[n].strip() for n in range(2)]
		final = final.split(":")
		if len(final) != 2:
			continue
		if final[0] < final[1]:
			a = teams[0]
			teams[0] = teams[1]
			teams[1] = a
			a = final[0]
			final[0] = final[1]
			final[1] = a
			a = odds[0]
			odds[0] = odds[1]
			odds[1] = a 
			
		odds_table.append([event[0],year,teams[0],teams[1],odds[0],odds[1]])

def change(str):
	s = str.lower().strip().split("-")
	return " ".join(s)
	
SCROLL_PAUSE_TIME = 4

URL_START = "https://www.oddsportal.com" 
URL_SITE = "/tennis/results/"

venues = np.loadtxt('atpvenues.csv', delimiter=";",dtype=np.dtype(str)).tolist()
game_results = np.loadtxt("atpresults.csv", delimiter=",", dtype=np.dtype(str),  encoding="utf-8")
game_results = game_results.tolist()

odds_table = []

games = {}

browser = webdriver.Chrome()

try:
	new_game_results = open("atpresults4.csv","w", encoding='utf-8')
	
	
	url = URL_START + URL_SITE
	browser.get(url)
	
	innerHTML = "<html><body>"
	innerHTML += browser.execute_script("return document.getElementById(\"col-content\").innerHTML")
	innerHTML += "</body></html>"

	season = html.fromstring(innerHTML)
	
	for i in range(len(venues)):
		s = "ATP " + venues[i][1]
		venue = season.xpath('.//a[contains(text(),"{}")]'.format(s))
		link = venue[0].attrib['href']
		venues[i].append(link)
	
	for event in venues:
		link = event[2]
		time.sleep(SCROLL_PAUSE_TIME)
		(inn,season) = pre_scrape(link)
		
		this_season = inn.xpath('.//span[@class="active"]')
		data = [this_season[i].text_content() for i in range(len(this_season))]
		
		
		year = data[1]
		if ("2018" in data):
			scrape(season)
		elif ("2017" not in data):
			time.sleep(SCROLL_PAUSE_TIME)
			year = "2018"
			link = link_maker(event[2],year)
			(inn,season) = pre_scrape(link)
			scrape(season)
		else:
			scrape(season)
		year = "2017"
		time.sleep(SCROLL_PAUSE_TIME)
		link = link_maker(event[2],year)
		(inn,season) = pre_scrape(link)
		scrape(season)
	gr_df = pd.DataFrame(game_results)
	gr_df.head()
	# years = ["2017", "2018"]
	# points = ["500","1000","2000"]
	# table = gr_df.loc[(~gr_df[2].isin(years) & ~gr_df[1].isin(points))]
	for row in odds_table:
		# game = table.loc[(table[2] == row[1]) & (table[0] == row[0]) & (table[5].str.contains(row[3]))]
		# print(game)
		# " ".join(gr_df[5].str.lower().str.strip().split("-")).str.contains(" ".join(row[3].lower().strip().split("-")))
		'''
		game = gr_df.loc[(gr_df[2] == row[1]) & \
		(gr_df[0] == row[0]) & \
		((" ".join(gr_df[4].str.lower().str.strip().str.split("-"))).str.contains(" ".join(row[2].lower().strip().split("-")))) & \
		(" ".join(gr_df[5].str.lower().str.strip().str.split("-")).str.contains(" ".join(row[3].lower().strip().split("-"))))]
		'''
		game = gr_df.loc[(gr_df[2] == row[1]) & \
		(gr_df[0] == row[0]) & \
		(gr_df[4].map(change).str.contains(" ".join(row[2].lower().strip().split("-")))) & \
		(gr_df[5].map(change).str.contains(" ".join(row[3].lower().strip().split("-"))))]
		
		if game.empty:
			continue
		if len(game.index) > 1:
			print(game)
		game = game.iloc[0].tolist()
		game.append(str(row[4]))
		game.append(str(row[5]))
		# game.append(str(row[4]))
		# game.append(str(row[5]))
		# print(game)
		id = game[7]
		games[id] = game

	
	for game in game_results:
		if game[7] in games:
			game.append(games[game[7]][-2])
			game.append(games[game[7]][-1])
		else:
			game.append("0")
			game.append("0")
	
	# hardcoded mistake fix
	games = [49251,51644,51650,51653,51649,51648,55161,55181]
	bets = [[1.87,1.87],[3.11,1.36],[1.4,2.89],[1.83,1.98],[4.47,1.20],[1.36,3.18],[1.99,1.83],[2.4,1.55]]
	for num in range(len(games)):
		game_results[games[num]][-2] = str(bets[num][0])
		game_results[games[num]][-1] = str(bets[num][1])		
		print(game_results[games[num]])
	#...
	
	for row in game_results:
		new_game_results.write(";".join(row))
		new_game_results.write('\n')
	new_game_results.close()
finally:
		browser.close()