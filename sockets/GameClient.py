import socket
import threading
import json
class GameCLient:


    def __init__(self):
        self.client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.client.connect(("10.100.102.15", 55555))
        self.room=None
        self.symbol=None

        recive_thread = threading.Thread(target=self.recive)
        write_thread = threading.Thread(target=self.write)
        recive_thread.start()
        write_thread.start()

    def recive(self):
        while True:
            try:
                messsage=self.client.recv(2014).decode("utf-8")

                data=json.loads(messsage)
                if data["type"]=="GAME START":
                    print("Game has started!")
                    self.symbol=data["symbol"]
                    print(f'You are {self.symbol} player!')
                if data["type"]=="NOT_TURN":
                    print("its Not your turn!")
                if data["type"]=="SUCCES":
                    print("Moved succesfully")
                if data["type"]=="YOUR_TURN":
                    print("Its your turn!")
            except:
                print("An error had ouccured")
                self.client.close()
                break

    def write(self):
        while True:
            msg=f'{input()}'
            if msg=='1':
                msg={"type":"FIND_GAME"}
                print("Searching...")
                data=json.dumps(msg)
                self.client.send(data.encode("utf-8"))
            if msg=='2':
                msg={"type":"MOVE"}
                data=json.dumps(msg)
                self.client.send(data.encode("utf-8"))

print("[1]- search for a room")
g=GameCLient()