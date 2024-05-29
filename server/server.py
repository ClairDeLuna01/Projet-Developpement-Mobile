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

db = sqlite3.connect('tmp/db.sqlite3', check_same_thread=False)


db.execute('''
CREATE TABLE IF NOT EXISTS users (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL,
    passwordHash TEXT NOT NULL,
    email TEXT NOT NULL
);
''')

db.execute('''
CREATE TABLE IF NOT EXISTS Games (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    PLAYERS INTEGER[] NOT NULL,
    ROUND_MAX INTEGER NOT NULL,
    SENTENCES TEXT[] NOT NULL,
    DONE_PLAYERS BOOLEAN[] NOT NULL,
    ROUND_N INTEGER NOT NULL,
    DONE BOOLEAN NOT NULL,
    ROOM INTEGER NOT NULL,

    FOREIGN KEY (PLAYERS) REFERENCES users(ID),
    FOREIGN KEY (ROOM) REFERENCES rooms(ID)
);
''')


db.execute('''
CREATE TABLE IF NOT EXISTS Room (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    NAME TEXT NOT NULL,
    OWNER INTEGER NOT NULL,
    PASSWORD TEXT,
    PLAYERS INTEGER[] NOT NULL,
    GAME INTEGER,
    READY_PLAYERS BOOLEAN[] NOT NULL,
    ROUND_MAX INTEGER NOT NULL,

    FOREIGN KEY (OWNER) REFERENCES users(ID),
    FOREIGN KEY (PLAYERS) REFERENCES users(ID),
    FOREIGN KEY (GAME) REFERENCES games(ID) ON DELETE CASCADE
);
''')


@app.route('/db/login/<username>/<passwordHash>', methods=['GET'])
def login(username: str, passwordHash: str):
    cursor = db.cursor()
    cursor.execute(
        'SELECT * FROM users WHERE username=? AND passwordHash=?', (username, passwordHash))
    if (x := cursor.fetchone()) is not None:
        return json.dumps({'success': True, 'id': x[0]})
    return json.dumps({'success': False, 'id': -1})


@app.route('/db/register/<username>/<passwordHash>/<email>', methods=['GET'])
def register(username: str, passwordHash: str, email: str):
    cursor = db.cursor()
    cursor.execute(
        'INSERT INTO users (username, passwordHash, email) VALUES (?, ?, ?)', (username, passwordHash, email))
    db.commit()
    return json.dumps({'success': True, 'id': cursor.lastrowid})


@app.route('/db/getUser/<int:id>', methods=['GET'])
def getUser(id: int):
    cursor = db.cursor()
    cursor.execute('SELECT * FROM users WHERE ID=?', (id,))
    user = cursor.fetchone()
    if user is not None:
        return json.dumps({'success': True, 'username': user[1], 'email': user[3], 'id': user[0], 'passwordHash': user[2]})
    return json.dumps({'success': False, 'id': -1})


def fromBoolStr(s: str) -> bool:
    return s.lower() == 'true'


class Game:
    def __init__(self, id: int, players: str, round_max: int, sentences: str = "ARRAY[]", donePlayers: str = "ARRAY[]", roundNumber: int = 0, done: bool = False, roomId: int = -1):
        self.id = id
        self.players = list(map(int, players[6:-1].split(',')))
        self.roundMax = round_max
        self.sentences = list(map(str, [s.strip()
                              for s in sentences[6:-1].split(',')]))
        self.donePlayers = list(
            map(fromBoolStr, [s.strip() for s in donePlayers[6:-1].split(',')]))
        self.roundNumber = roundNumber
        self.done = done
        self.roomId = roomId

    def addSentence(self, player: int, sentence: str):
        playerIndex = self.players.index(player)
        if self.donePlayers[playerIndex]:
            return
        self.sentences[playerIndex] += sentence
        self.donePlayers[playerIndex] = True

    def isDone(self):
        return all(self.donePlayers)

    def tick(self):
        if self.isDone():
            # rotate sentences
            sentencesTmp: list[str] = self.sentences.copy()
            for i in range(len(self.players)):
                sentencesTmp[i] = self.sentences[(i+1) % len(self.players)]
                self.donePlayers[i] = False
            self.sentences = sentencesTmp
            self.roundNumber += 1
            if self.roundNumber >= self.roundMax:
                self.done = True
            return True
        return False

    def toJson(self):
        return {
            'id': self.id,
            'players': self.players,
            'roundMax': self.roundMax,
            'sentences': self.sentences,
            'donePlayers': self.donePlayers,
            'roundNumber': self.roundNumber,
            'done': self.done,
            'roomId': self.roomId
        }

    def update(self):
        cursor = db.cursor()
        cursor.execute('''
        UPDATE Games SET PLAYERS=?, ROUND_MAX=?, SENTENCES=?, DONE_PLAYERS=?, ROUND_N=?, DONE=? WHERE ID=?
                       ''', (
            f"ARRAY[{','.join(map(str, self.players))}]",
            self.roundMax,
            f"ARRAY[{','.join(map(str, [self.sentences[i] for i in range(len(self.players))]))}]",
            f"ARRAY[{','.join(map(str, [self.donePlayers[i] for i in range(len(self.players))]))}]",
            self.roundNumber, self.done, self.id
        ))
        db.commit()


class Room:
    def __init__(self, id: int, name: str, owner: int, password: Union[str, None], players: str = "ARRAY[]", gameID: Union[int, None] = None, readyPlayers: str = "ARRAY[]", roundMax: int = 0):
        self.id = id
        self.name = name
        self.owner = owner
        self.password = password
        self.players = list(map(int, players[6:-1].split(',')))
        self.gameID = gameID
        self.readyPlayers = list(
            map(fromBoolStr, [s.strip() for s in readyPlayers[6:-1].split(',')]))
        self.roundMax = roundMax

    def toJson(self):
        if self.gameID is None:
            return {
                'id': self.id,
                'name': self.name,
                'owner': self.owner,
                'players': self.players,
                'gameID': self.gameID,
                'readyPlayers': self.readyPlayers,
                'roundMax': self.roundMax,
                'game': None
            }
        else:
            cursor = db.cursor()
            gameStr = cursor.execute(
                'SELECT * FROM Games WHERE ID=?', (self.gameID,)).fetchone()
            game = Game(*gameStr)
            return {
                'id': self.id,
                'name': self.name,
                'owner': self.owner,
                'players': self.players,
                'gameID': self.gameID,
                'readyPlayers': self.readyPlayers,
                'roundMax': self.roundMax,
                'game': game.toJson()
            }

    def startGame(self):
        self.readyPlayers = [False for _ in self.players]

        cursor = db.cursor()
        cursor.execute('''
        INSERT INTO Games (PLAYERS, ROUND_MAX, SENTENCES, DONE_PLAYERS, ROUND_N, DONE, ROOM) VALUES (?, ?, ?, ?, ?, ?, ?)
                       ''', (
            f"ARRAY[{','.join(map(str, self.players))}]",
            self.roundMax,
            f"ARRAY[{','.join(map(str, ['' for _ in self.players]))}]",
            f"ARRAY[{','.join(map(str, [False for _ in self.players]))}]",
            0,
            False,
            self.id
        ))

        self.gameID = cursor.lastrowid

    def update(self):
        cursor = db.cursor()
        cursor.execute('''
        UPDATE Room SET NAME=?, OWNER=?, PASSWORD=?, PLAYERS=?, GAME=?, READY_PLAYERS=?, ROUND_MAX=? WHERE ID=?
                          ''', (
            self.name,
            self.owner,
            self.password,
            f"ARRAY[{','.join(map(str, self.players))}]",
            self.gameID,
            f"ARRAY[{','.join(map(str, self.readyPlayers))}]",
            self.roundMax,
            self.id
        ))
        db.commit()


@app.route('/rooms/createRoom/<int:owner>/<name>/<password>', methods=['GET'])
def createRoomPwd(owner: int, name: str, password: str):
    cursor = db.cursor()

    cursor.execute('INSERT INTO Room (NAME, OWNER, PASSWORD, PLAYERS, READY_PLAYERS, ROUND_MAX) VALUES (?, ?, ?, ?, ?, ?)',
                   (name, owner, password, f"ARRAY[{owner}]", f"ARRAY[False]", 8))

    cursor.execute('SELECT * FROM Room WHERE NAME=? AND OWNER=? AND PASSWORD=? AND PLAYERS=? AND READY_PLAYERS=? AND ROUND_MAX=?',
                   (name, owner, password, f"ARRAY[{owner}]", f"ARRAY[False]", 8))
    roomTxt = cursor.fetchone()
    room = Room(*roomTxt)

    db.commit()

    return json.dumps({'success': True, 'room': room.toJson()})


@app.route('/rooms/createRoom/<int:owner>/<name>', methods=['GET'])
def createRoomNoPwd(owner: int, name: str):
    cursor = db.cursor()

    cursor.execute('INSERT INTO Room (NAME, OWNER, PLAYERS, READY_PLAYERS, ROUND_MAX) VALUES (?, ?, ?, ?, ?)',
                   (name, owner, f"ARRAY[{owner}]", f"ARRAY[False]", 8))

    cursor.execute('SELECT * FROM Room WHERE NAME=? AND OWNER=? AND PLAYERS=? AND READY_PLAYERS=? AND ROUND_MAX=?',
                   (name, owner, f"ARRAY[{owner}]", f"ARRAY[False]", 8))
    roomTxt = cursor.fetchone()
    room = Room(*roomTxt)

    db.commit()

    return json.dumps({'success': True, 'room': room.toJson()})


@app.route('/rooms/getRoom/<int:id>', methods=['GET'])
def getRoom(id: int):
    cursor = db.cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomtxt in rooms:
        room = Room(*roomtxt)
        if room.id == id:
            return json.dumps({'success': True, 'room': room.toJson()})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/getRooms', methods=['GET'])
def getRooms():
    cursor = db.cursor()
    roomsRaw = cursor.execute('SELECT * FROM Room').fetchall()
    rooms: list[dict] = []
    for roomStr in roomsRaw:
        roomTxt = roomStr[1]
        room = Room(*roomTxt)

        rooms.append(room.toJson())
    return json.dumps({'success': True, 'rooms': rooms})


@app.route('/rooms/joinRoom/<int:player>/<int:roomId>/<password>', methods=['GET'])
def joinRoomPwd(player: int, roomId: int, password: str):
    cursor = db.cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        room = Room(*roomStr)
        if room.id == roomId:
            if room.password is not None and room.password == password and player not in room.players:
                room.players.append(player)
                room.readyPlayers.append(False)
                room.update()
                return json.dumps({'success': True, 'room': room.toJson()})
            else:
                return json.dumps({'success': False, 'room': None})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/joinRoom/<int:player>/<int:roomId>', methods=['GET'])
def joinRoomNoPwd(player: int, roomId: int):
    cursor = db.cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        room = Room(*roomStr)
        if room.id == roomId and room.password is None:
            room.players.append(player)
            room.readyPlayers.append(False)
            room.update()
            return json.dumps({'success': True, 'room': room.toJson()})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/leaveRoom/<int:player>/<int:roomId>', methods=['GET'])
def leaveRoom(player: int, roomId: int):
    cursor = db.cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        room = Room(*roomStr)
        if room.id == roomId:
            room.players.remove(player)
            if len(room.players) == 0:
                cursor.execute('DELETE FROM Room WHERE ID=?', (room.id,))
                return json.dumps({'success': True, 'room': None})

            room.update()
            return json.dumps({'success': True, 'room': room.toJson()})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/ready/<int:player>/<int:roomId>', methods=['GET'])
def ready(player: int, roomId: int):
    cursor = db.cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        room = Room(*roomStr)
        if room.id == roomId:
            playerIndex = room.players.index(player)
            room.readyPlayers[playerIndex] = True
            if all(room.readyPlayers):
                room.startGame()
            room.update()
            return json.dumps({'success': True, 'room': room.toJson()})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/addSentence/<int:player>/<int:roomId>/<sentence>', methods=['GET'])
def addSentence(player: int, roomId: int, sentence: str):
    cursor = db.cursor()
    rooms = cursor.execute('SELECT * FROM Room').fetchall()
    for roomStr in rooms:
        room = Room(*roomStr)
        if room.id == roomId and room.gameID is not None and player in room.players:
            gameStr = cursor.execute(
                'SELECT * FROM Games WHERE ID=?', (room.gameID,)).fetchone()
            game = Game(*gameStr)
            game.addSentence(player, sentence)
            game.tick()

            game.update()

            return json.dumps({'success': True, 'room': room.toJson()})
    return json.dumps({'success': False, 'room': None})


@app.route('/rooms/clearRooms', methods=['GET'])
def clearRooms():
    cursor = db.cursor()
    cursor.execute('DELETE FROM Room')
    db.commit()
    return json.dumps({'success': True})


def save():
    db.commit()


atexit.register(save)

if __name__ == '__main__':
    app.run(debug=True)
