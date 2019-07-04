import selenium as se
from selenium import webdriver
import time
from lxml import html
from lxml.etree import tostring
import os.path
import numpy as np
import pandas as pd

def pre_scrape(link):

	url = URL_START + link
	browser.get(url)
	innerHTML = "<html><body>"
	innerHTML += browser.execute_script("return document.getElementById(\"col-content\").innerHTML")
	innerHTML += "</body></html>"
	
	season = html.fromstring(innerHTML)
	return season
	
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
		teams = [teams[n].split(" ") for n in range(2)]
		for n in range(2):
			if teams[n][-1][-1] == ".":
				teams[n][-1] = ""
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
		
		print('{}\t{}\t{}'.format(teams, final, odds))
		odds_table.append([event[0],year,teams[0],teams[1],odds[0],odds[1]])

	
SCROLL_PAUSE_TIME = 2

URL_START = "https://www.oddsportal.com" 
URL_SITE = "/tennis/results/"

venues = np.loadtxt('atpvenues.csv', delimiter=";",dtype=np.dtype(str)).tolist()
game_results = np.loadtxt("atpresults.csv", delimiter=",", dtype=np.dtype(str),  encoding="utf-8")
print(game_results[:5])

odds_table = []

games = {}

browser = webdriver.Chrome()

try:
	url = URL_START + URL_SITE
	browser.get(url)
	
	innerHTML = "<html><body>"
	innerHTML += browser.execute_script("return document.getElementById(\"col-content\").innerHTML")
	innerHTML += "</body></html>"

	season = html.fromstring(innerHTML)
	
	for i in range(len(venues)):
		str = "ATP " + venues[i][1]
		venue = season.xpath('.//a[contains(text(),"{}")]'.format(str))
		link = venue[0].attrib['href']
		venues[i].append(link)
	
	for event in [venues[0]]:
		link = event[2]
		time.sleep(SCROLL_PAUSE_TIME)
		season = pre_scrape(link)
		
		this_season = season.xpath('.//span[@class="active"]')
		data = [this_season[i].text_content() for i in range(len(this_season))]
		
		year = data[1]
		if ("2018" in data):
			scrape(season)
		elif ("2017" not in data):
			time.sleep(SCROLL_PAUSE_TIME)
			year = "2018"
			link = link_maker(event[2],year)
			season = pre_scrape(link)
			scrape(season)
		
		year = "2017"
		time.sleep(SCROLL_PAUSE_TIME)
		link = link_maker(event[2],year)
		season = pre_scrape(link)
		scrape(season)
	gr_df = pd.DataFrame(game_results)
	gr_df.head()
	
	years = ["2017", "2018"]
	points = ["500","1000","2000"]
	table = gr_df.loc[(~gr_df[2].isin(years) & ~gr_df[1].isin(points))]
	
	print(table.head())
	for row in odds_table:
		print(row)
		print(table.loc[lambda df: df[2] == '2018'].head())
		game = table.loc[(table[2] == row[1]) & (table[0] == row[0]) & (row[3] in table[5])]
		print(game)
		if game.empty:
			continue
		game.append(str(row[4]))
		game.append(str(row[5]))
		id = game[7]
		games[id] = game
		print(game)
	
	for game in game_results:
		if game[7] in games:
			game.append(games[game[7]][-2])
			game.append(games[game[7]][-1])
		else:
			game.append("0")
			game.append("0")
	
	new_game_results = open("atpresults3.csv","w")
	for row in game_results:
		new_game_results.write(";".join(row))
		new_game_results.write('\n')
	new_game_results.close()
finally:
		browser.close()