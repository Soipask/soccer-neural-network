import selenium as se
from selenium import webdriver
import time
from lxml import html
import os.path
import sys
import re

def get_date(date):
	list = date.split(" ")
	day = list[0]
	month = list[1]
	year = list[2]
	
	if month == "Jan":
		month = "01"
	elif month == "Feb":
		month = "02"
	elif month == "Mar":
		month = "03"
	elif month == "Apr":
		month = "04"
	elif month == "May":
		month = "05"
	elif month == "Jun":
		month = "06"
	elif month == "Jul":
		month = "07"
	elif month == "Aug":
		month = "08"
	elif month == "Sep":
		month = "09"
	elif month == "Oct":
		month = "10"
	elif month == "Nov":
		month = "11"
	elif month == "Dec":
		month = "12"
	
	ret_val = year + '-' + month + '-' + day
	return ret_val
def get_league(cou):
	url_backup = ""
	if cou.upper() == "ENG":
		url_league = "/soccer/england/premier-league"
		country = "ENGLAND"
		cou = "eng"
	elif cou.upper() == "GER":
		url_league = "/soccer/germany/bundesliga"
		country = "GERMANY"
		cou = "ger"	
	elif cou.upper() == "SPA":
		url_league = "/soccer/spain/laliga"
		url_backup = "/soccer/spain/primera-division"
		country = "SPAIN"
		cou = "spa"
	elif cou.upper() == "BUL":
		url_league = "/soccer/bulgaria/parva-liga"
		country = "BULGARIA"
		url_backup = "/soccer/bulgaria/a-pfg"
		cou = "bul"
	elif cou.upper() == "QAT":
		url_league = "/soccer/qatar/premier-league"
		country = "QATAR"
		cou = "qat"
	else:
		raise ValueError('Bad command line argument.')
	
	return (url_league,url_backup,country,cou)

SCROLL_PAUSE_TIME = 5

# preparing database
team_dict = []
team_ids = [""]
if os.path.isfile("teamdatabase.csv"):
	id_file = open("teamdatabase.csv")
	team_ids = id_file.read()
	team_ids1 = team_ids.split("\n")
	team_ids = [team_ids1[n].split(";") for n in range(len(team_ids1))]
	team_dict = [None] * (len(team_ids) - 1)

	for i in range(len(team_ids) - 1):
		team_dict[int(team_ids[i][1])] = team_ids[i][0]

	id_file.close()
# The scraping

(URL_LEAGUE,URL_BACKUP,COUNTRY,COU) = get_league(sys.argv[1])

URL_SITE = "https://www.oddsportal.com"
URL_END = "/results/"
URL_NEW_PAGE_END = "#/page/"

browser = webdriver.Chrome()

# getting all seasons
url = URL_SITE + URL_LEAGUE + URL_END
browser.get(url)
innerHTML_seasons = "<html><body>"
innerHTML_seasons += browser.execute_script("return document.getElementById(\"col-content\").innerHTML")
innerHTML_seasons += "</body></html>"

doc_season = html.fromstring(innerHTML_seasons)
row_seasons = doc_season.xpath("//ul[contains(@class,\"main-filter\")]")
row_seasons = row_seasons[1].getchildren()
seasons = [row_seasons[n].text_content() for n in range(len(row_seasons))] 

game_results = open("new2" + COU + ".csv",'a')


# season scraping
league = URL_LEAGUE
gamenr = 10000
add = True
if seasons[0] == "2018/2019":		# sometimes new season already has a new page allocated, sometimes not and it affects url
	add = False
for season in seasons:
	urlseason = ""
	if add:									
		urlseason = '-'+'-'.join(season.split('/'))
	else:
		add = True
	if int(season.split('/')[1]) > 2019:
		continue
	
	if (int(season.split("/")[0]) < 2016 ) & ((COUNTRY == "SPAIN")|(COUNTRY == "BULGARIA")):
		league = URL_BACKUP
	
	url = URL_SITE + league + urlseason + URL_END

	browser.get(url)
	# sometimes the site has season listed, but has "No data available" message with DOM id emptyMsg
	null = browser.execute_script("return document.getElementById(\"emptyMsg\")")
	if null != None:
		break
	innerHTML = "<html><body>"
	time.sleep(SCROLL_PAUSE_TIME)
	innerHTML += browser.execute_script("return document.getElementById(\"tournamentTable\").innerHTML")
	MAXPAGE = browser.execute_script("return document.getElementById(\"pagination\").lastChild.getAttribute(\"x-page\")")

	i = 2
	while i <= int(MAXPAGE) :
		url = URL_SITE + league + urlseason + URL_END + URL_NEW_PAGE_END + str(i) + "/"
		browser.get(url)
		time.sleep(SCROLL_PAUSE_TIME)
		innerHTML += browser.execute_script("return document.getElementById(\"tournamentTable\").innerHTML")
		i += 1

	# while browser.execute_script("return document.getElementById(\"tournament-page-results-more\").style.display") != "none":
	#	button = browser.find_element_by_link_text("Show more matches")
	#	browser.execute_script("arguments[0].scrollIntoView(true);", button)
	#	button.click()
	#	time.sleep(SCROLL_PAUSE_TIME)
	# innerHTML = browser.execute_script("return document.getElementById(\"fs-results\").innerHTML")

	innerHTML += "</body></html>"
	doc = html.fromstring(innerHTML)
	rows = doc.xpath("//tr[contains(@class,\"deactivate\") or contains(@class,\"nob-border\")]")

	
	for j in range(len(rows)):
		types = rows[j].get("class").split(' ')
		if "nob-border" in types:
			date = rows[j].getchildren()[0].text_content()
			date = get_date(date)
		else:
			children = rows[j].getchildren()
			game_time = children[0].text_content()
			match = children[1].text_content()
			final = children[2].text_content()
			odds = [children[n].text_content() for n in range(3,6)]
			teams = match.split(" - ")
			teams = [teams[n].strip() for n in range(2)]
			final = final.split(":")
			id = [0,0]
			if(len(final) != 2):
				continue
			if re.search('[a-zA-Z]', final[1]):
				if(int(final[0][0]) > int(final[1][0])):
					final[0] = str(int(final[0][0])-1)
					final[1] = final[1][0]
				else:
					final[1] = str(int(final[1][0])-1)
				print(final)
			for n in range(2):	
				if teams[n] in team_dict:
					id[n] = team_dict.index(teams[n])
				else:
					id[n] = len(team_dict)
					team_dict.append(teams[n])
			
			print('{}\t{}\t{}\t{}\t{}\t{}'.format(teams, gamenr, 0, id, final, date, odds))
			game_results.write('{};{};{};{};{};{};{};{};{};{};{};{};{}\n'
				.format(teams[0],teams[1],gamenr,0,id[0],id[1],final[0],final[1],date,season,odds[0],odds[1],odds[2]))
			gamenr += 1
		
print(team_dict)


if len(team_dict) > len(team_ids) - 1:
	id_file = open("teamdatabase.csv",'a')
	for n in range(len(team_ids) - 1,len(team_dict)):
		id_file.write('{};{};{}\n'.format(team_dict[n],n,COUNTRY))
	id_file.close()
	
game_results.close()

browser.close()
	
	
# assign dictionary number:
# 	make csv, empty -
#	always at the start of program open and read -
# 	make array indexed by id from it -
#	all new additions throw into array (remember like the highest id) 
#		// append (it's going numerically and len(team_id) - 1 is the id of maximum already saved
#	close dictionary.csv and then open for append
#	write new ids along with teams

# finishing touches... Homestring, Awaystring, GameNr, Round, HomeID, AwayID, Homefinal, Awayfinal, Date, Homeodds, Drawodds, Awayodds