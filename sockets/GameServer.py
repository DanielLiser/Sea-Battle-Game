import socket
import threading
import json
import time

#from Client import client
#from operator import index

#from Client import client
from GameRoom import GameRoom

HOST="127.0.0.1"
PORT=55555

clients=[]
nickNames=[]
waiting=[]
client_rooms={}


server = socket.socket(socket.AF_INET,socket.SOCK_STREAM)
server.bind((HOST,PORT))
server.listen()
print("Server is on..")

def brodcast(message):
    for client in clients:
        client.send(message)

def brodcast2(message,spical):
    for client in clients:
        if client!=spical:
            client.send(message)

def playerMove(message):
    for client in clients:
        client.send(message)

# def sendToRoom(message,room):
#     msg=json.dumps(message)
#     if room.player1:
#         room.player1.send(message)
#     if room.player2:
#         room.player2.send(message)


def createRoom(player1, player2):
    room=GameRoom(player1,player2)
    room.player1=player1
    room.player2=player2


    return room


def sendJson(player, msg):
    data=json.dumps(msg)
    player.send(data.encode("utf-8"))


def startGame(currentRoom, player1, player2):
    client_rooms[player1] = currentRoom
    client_rooms[player2] = currentRoom


    msg1 = {
        "type": "GAME START",
        "symbol": 1
    }
    sendJson(player1, msg1)
    msg2 = {
        "type": "GAME START",
        "symbol": 2
    }
    sendJson(player2, msg2)
    time.sleep(5)
    currentRoom.startNewRound()


def handle(client):
    while True:
        try:
            msg = client.recv(1024).decode("utf-8")
            data = json.loads(msg)

            if data["type"] == "FIND_GAME":
                print("JOINED!")
                if client not in waiting:
                    waiting.append(client)
                if len(waiting) >= 2:
                    player1 = waiting.pop(0)
                    player2 = waiting.pop(0)
                    currentRoom = createRoom(player1, player2)
                    startGame(currentRoom, player1, player2)

            elif data["type"] == "MOVE":
                # --- התיקון כאן ---
                # שולפים את החדר המשויך לשחקן הספציפי ששלח את ההודעה
                if client in client_rooms:
                    room = client_rooms[client]
                    room.handleTurn(client, data)
                else:
                    print("Error: Move received but client is not in any room!")

        except Exception as e:
            # מעכשיו לא נקבל רק "ERROR!", אלא נראה את הבעיה האמיתית
            print(f"ERROR or Client Disconnected: {e}")
            break  # יוצאים מהלולאה כדי לסגור את הת'רד אם השחקן התנתק




def recive():
    count=0
    while True:
        client, adress=server.accept()
        clients.append(client)

        thread= threading.Thread(target=handle,args=(client,))
        thread.start()
        count+=1

recive()