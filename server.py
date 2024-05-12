from flask import Flask, render_template, request, redirect, url_for, flash, send_file
from http import HTTPStatus as status
from os import urandom
import requests
import sqlite3
import json
from typing import Union
import atexit

app = Flask(__name__)
app.secret_key = urandom(32)

db = sqlite3.connect('tmp/db.sqlite3')

app.jinja_env.globals['db'] = db

db.execute('''
CREATE TABLE IF NOT EXISTS users (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL,
    passwordHash TEXT NOT NULL,
    email TEXT NOT NULL
);
''')

# temporarely hold a json string of a room object
# to avoid having to duplicate code but will be fixed later
db.execute('''
CREATE TABLE IF NOT EXISTS Room (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    JSON TEXT NOT NULL
);
''')


@app.route('/db/login/<username>/<passwordHash>', methods=['GET'])
def login(username: str, passwordHash: str):
    cursor = app.jinja_env.globals['db'].cursor()
    cursor.execute(
        'SELECT * FROM users WHERE username=? AND passwordHash=?', (username, passwordHash))
    if (x := cursor.fetchone()) is not None:
        return json.dumps({'success': True, 'id': x[0]})
    return json.dumps({'success': False, 'id': -1})


@app.route('/db/register/<username>/<passwordHash>/<email>', methods=['GET'])
def register(username: str, passwordHash: str, email: str):
    cursor = app.jinja_env.globals['db'].cursor()
    cursor.execute(
        'INSERT INTO users (username, passwordHash, email) VALUES (?, ?, ?)', (username, passwordHash, email))
    db.commit()
    return json.dumps({'success': True, 'id': cursor.lastrowid})


@app.route('/db/getUser/<int:id>', methods=['GET'])
def getUser(id: int):
    cursor = app.jinja_env.globals['db'].cursor()
    cursor.execute('SELECT * FROM users WHERE ID=?', (id,))
    user = cursor.fetchone()
    if user is not None:
        return json.dumps({'success': True, 'username': user[1], 'email': user[3], 'id': user[0], 'passwordHash': user[2]})
    return json.dumps({'success': False, 'id': -1})


class GameParameters:
    def __init__(self, roundNumber: int):
        self.roundNumber = roundNumber


class Game:
    def __init__(self, players: list[int], parameters: GameParameters, sentences: dict[int, str] = {}, donePlayers: dict[int, bool] = {}, roundNumber: int = 0, done: bool = False):
        self.players = players
        self.parameters = parameters
        self.sentences = sentences if sentences != {} else {
            player: "" for player in players}
        self.donePlayers = donePlayers if donePlayers != {} else {
            player: False for player in players}
        self.roundNumber = roundNumber
        self.done = done

    def toJson(self):
        return {
            'players': self.players,
            'parameters': self.parameters.__dict__,
            'sentences': self.sentences,
            'done': self.donePlayers,
            'roundNumber': self.roundNumber,
            'done': self.done
        }

    def addSentence(self, player: int, sentence: str):
        self.sentences[player] = sentence
        self.donePlayers[player] = True

    def isDone(self):
        return all(self.donePlayers.values())

    def tick(self):
        if self.isDone():
            # rotate sentences
            for i in range(len(self.players)):
                self.sentences[self.players[i]] = self.sentences[self.players[(
                    i + 1) % len(self.players)]]
                self.donePlayers[self.players[i]] = False
            self.roundNumber += 1
            if self.roundNumber >= self.parameters.roundNumber:
                self.done = True
            return True
        return False


class Room:
    def __init__(self, name: str, owner: int, password: Union[str, None], players: list[int] = [], game: Union[Game, None] = None, readyPlayers: list[bool] = [False]):
        self.id = 0
        self.name = name
        self.owner = owner
        self.password = password
        self.players = players if players != [] else [owner]
        self.game = game
        self.readyPlayers = readyPlayers

    def toJson(self):
        return {
            'id': self.id,
            'name': self.name,
            'owner': self.owner,
            'players': self.players,
            'game': self.game.toJson() if self.game is not None else None,
            'readyPlayers': self.readyPlayers
        }

    def toJsonNoID(self):
        return {
            'name': self.name,
            'owner': self.owner,
            'players': self.players,
            'game': self.game.toJson() if self.game is not None else None,
            'readyPlayers': self.readyPlayers
        }

    def startGame(self, parameters: GameParameters):
        self.game = Game(self.players, parameters)
        self.readyPlayers = [False for _ in self.players]


parameters = GameParameters(roundNumber=3)


@app.route('/rooms/createRoom/<int:owner>/<name>/<password>', methods=['GET'])
def createRoomPwd(owner: int, name: str, password: str):
    cursor = app.jinja_env.globals['db'].cursor()
    room = Room(name, owner, password)

    cursor.execute('INSERT INTO Room (JSON) VALUES (?)',
                   (json.dumps(room.toJsonNoID()),))

    db.commit()

    return json.dumps({'success': True, 'room': room.toJsonNoID()})


@app.route('/rooms/createRoom/<int:owner>/<name>', methods=['GET'])
def createRoomNoPwd(owner: int, name: str):
    cursor = app.jinja_env.globals['db'].cursor()
    room = Room(name, owner, None)

    cursor.execute('INSERT INTO Room (JSON) VALUES (?)',
                   (json.dumps(room.toJsonNoID()),))

    db.commit()

    return json.dumps({'success': True, 'room': room.toJsonNoID()})


@app.route('/rooms/getRoom/<int:id>', methods=['GET'])
def getRoom(id: int):
    cursor = app.jinja_env.globals['db'].cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for room in rooms:
        if room[0] == id:
            return json.dumps({'success': True, 'room': room[1]})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/getRooms', methods=['GET'])
def getRooms():
    cursor = app.jinja_env.globals['db'].cursor()
    roomsRaw = cursor.execute('SELECT * FROM Room').fetchall()
    rooms = []
    for roomStr in roomsRaw:
        roomDict = json.loads(roomStr[1])
        if not "password" in roomDict.keys():
            roomDict["password"] = None
        room = Room(**roomDict)
        room.id = roomStr[0]
        rooms.append(room.toJson())
    return json.dumps({'success': True, 'rooms': rooms})


@app.route('/rooms/joinRoom/<int:player>/<int:roomId>/<password>', methods=['GET'])
def joinRoomPwd(player: int, roomId: int, password: str):
    cursor = app.jinja_env.globals['db'].cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        if roomStr[0] == roomId:
            roomDict = json.loads(roomStr[1])
            if not "password" in roomDict.keys():
                roomDict["password"] = None
            room = Room(**roomDict)
            room.id = roomStr[0]

            if room.password is not None and room.password == password:
                return json.dumps({'success': True, 'room': room.toJson()})
            else:
                return json.dumps({'success': False, 'room': None})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/joinRoom/<int:player>/<int:roomId>', methods=['GET'])
def joinRoomNoPwd(player: int, roomId: int):
    cursor = app.jinja_env.globals['db'].cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        roomDict = json.loads(roomStr[1])
        if not "password" in roomDict.keys():
            roomDict["password"] = None
        room = Room(**roomDict)
        room.id = roomStr[0]
        if room.id == roomId and room.password is None:
            room.players.append(player)
            return json.dumps({'success': True, 'room': room.toJson()})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/leaveRoom/<int:player>/<int:roomId>', methods=['GET'])
def leaveRoom(player: int, roomId: int):
    cursor = app.jinja_env.globals['db'].cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        roomDict = json.loads(roomStr[1])
        if not "password" in roomDict.keys():
            roomDict["password"] = None
        room = Room(**roomDict)
        room.id = roomStr[0]
        if room.id == roomId:
            room.players.remove(player)
            if len(room.players) == 0:
                rooms.remove(room)
                return json.dumps({'success': True, 'room': None})
            return json.dumps({'success': True, 'room': room.toJson()})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/ready/<int:player>/<int:roomId>', methods=['GET'])
def ready(player: int, roomId: int):
    cursor = app.jinja_env.globals['db'].cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        roomDict = json.loads(roomStr[1])
        if not "password" in roomDict.keys():
            roomDict["password"] = None
        room = Room(**roomDict)
        room.id = roomStr[0]
        if room.id == roomId:
            room.readyPlayers[player] = True
            if all(room.readyPlayers):
                room.startGame(parameters)
            return json.dumps({'success': True, 'room': room.toJson()})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/addSentence/<int:player>/<int:roomId>/<sentence>', methods=['GET'])
def addSentence(player: int, roomId: int, sentence: str):
    cursor = app.jinja_env.globals['db'].cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        roomDict = json.loads(roomStr[1])
        if not "password" in roomDict.keys():
            roomDict["password"] = None
        room = Room(**roomDict)
        room.id = roomStr[0]
        if room.id == roomId and room.game is not None and player in room.players:
            room.game.addSentence(player, sentence)
            room.game.tick()
            return json.dumps({'success': True, 'room': room.toJson()})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/clearRooms', methods=['GET'])
def clearRooms():
    cursor = app.jinja_env.globals['db'].cursor()
    cursor.execute('DELETE FROM Room')
    db.commit()
    return json.dumps({'success': True})


def save():
    db.commit()


atexit.register(save)

if __name__ == '__main__':
    app.run(debug=True)
