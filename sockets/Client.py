import socket
import threading


client= socket.socket(socket.AF_INET,socket.SOCK_STREAM)
client.connect(("10.100.102.15",55555))

nickNameToChoose=input("Please choose a nickname: ")

def recive():
    while True:
        try:
            messsage=client.recv(2014).decode("utf-8")
            if messsage=="NICKNAME":
                client.send(nickNameToChoose.encode("utf-8"))
            else:
                print(messsage)
        except:
            print("An error had ouccured")
            client.close()

def write():
    while True:
        msg=f'{nickNameToChoose}: {input('')}'
        client.send(msg.encode("utf-8"))

recive_thread=threading.Thread(target=recive)
write_thread=threading.Thread(target=write)
recive_thread.start()
write_thread.start()