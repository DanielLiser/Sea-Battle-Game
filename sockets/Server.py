import socket
import threading
from operator import index

HOST="10.100.102.15"
PORT=55555

clients=[]
nickNames=[]


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

def handle(client):
    while True:
        try:
            msg=client.recv(1024)
            brodcast(msg)
        except:
            index=clients.index(client)
            clients.remove(client)
            nick=nickNames[index]
            nickNames.remove(nick)
            brodcast(f"{nick} has left the server")
            client.close()




def recive():
    while True:
        client, adress=server.accept()
        print(f"{str(adress)} is connected!")
        client.send(f"NICKNAME".encode("utf-8"))
        clients.append(client)
        nickname=client.recv(1024)
        nickNames.append(nickname.decode("utf-8"))
        client.send("You have connected to server! \n".encode("utf-8"))

        brodcast2(f"{nickname.decode("utf-8")} has joined!".encode("utf-8"),client)

        thread= threading.Thread(target=handle,args=(client,))
        thread.start()

recive()