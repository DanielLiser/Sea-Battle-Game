import threading
import time
import json
from pydoc import resolve

class GameRoom:

    def __init__(self,player1,player2):
        self.player1=player1
        self.player2=player2
        self.p1Move = None
        self.p2Move = None
        self.player1Turn=True
        self.player2Turn=False
        self.roundDuration=30
        self.isRoundActive=False
        #self.moves={}
        self.p1_hp=3
        self.p2_hp=3
        self.p1Ability="NONE"
        self.p2Ability="NONE"
        self.p1Target = -1
        self.p2Target = -1
        self.collision_event=False
        self.mines={}
        self.roundEvent = threading.Event()



    def startNewRound(self):
        self.p1Move = None
        self.p2Move = None
        self.isRoundActive=True
        self.broadcast({"type":"ROUND START","time":self.roundDuration})
        self.roundEvent.clear()
        threading.Thread(target=self.timer_logic).start()

    def handleTurn(self, client, moveData):
        print(f"Received move {moveData} from a client.")
        index=moveData["index"]
        ability=moveData.get("abillity","NONE")
        target=moveData.get("abilityTargetCell",-1)

        if (client == self.player1):
            self.p1Move = index
            self.p1Ability=ability
            self.p1Target=target
            print("Updated Player 1 move.")
        elif client == self.player2:
            self.p2Move = index
            self.p2Ability = ability
            self.p2Target = target
            print("Updated Player 2 move.")

        #print(f"Current State -> P1: {self.p1Move}, P2: {self.p2Move}")

        if (self.p1Move is not None and self.p2Move is not None):
            print("Both moves received! Broadcasting TURN_RESULT...")
            self.resolveTurn()

    def resolveTurn(self):
        print("Resolving turn, stopping timer...")
        self.roundEvent.set()
        self.isRoundActive = False

        if self.p1Move == self.p2Move and self.p1Move != -1:
            print("COLLISION! Both players hit each other.")
            self.p1_hp -= 1
            self.p2_hp -= 1
            self.collision_event = True

        #PLACE MINES
        if(self.p1Ability=="MINE"):
            self.mines[self.p1Target]="p1"
        if (self.p2Ability == "MINE"):
            self.mines[self.p2Target] = "p2"

      #CHECK STEPPED MINES
        p1_hit_mine_flag = False
        p2_hit_mine_flag = False

        if(self.p1Move in self.mines and self.mines[self.p1Move]=="p2"):
            if(self.p1Ability=="SHIELD"):
                pass
            #animate shield
            else:
                self.p1_hp-=1
                p1_hit_mine_flag=True
            del self.mines[self.p1Move]


        if (self.p2Move in self.mines and self.mines[self.p2Move] == "p1"):
            if (self.p2Ability == "SHIELD"):
                pass
            # animate shield
            else:
                self.p2_hp -= 1
                p2_hit_mine_flag=True

            del self.mines[self.p2Move]

        #CHECK MISSLE HITS


        if(self.p1Ability=="MISSLE"):
            if(self.p2Move==self.p1Target and self.p2Ability!="SHIELD"):
                self.p2_hp-=1
            elif(self.p2Move==self.p1Target and self.p2Ability=="SHIELD"):
                pass
            #animate shield
            else:
                pass
            #animate water hit
        if (self.p2Ability == "MISSLE"):
            if (self.p1Move == self.p2Target and self.p1Ability != "SHIELD"):
                self.p1_hp -= 1
            elif (self.p1Move == self.p2Target and self.p1Ability == "SHIELD"):
                pass
            # animate shield
            else:
                pass
            # animate water hit

        dataToSend = {
            "type": "TURN_RESULT",
            "p1Move": self.p1Move,
            "p2Move": self.p2Move,
            "p1_hp": self.p1_hp,
            "p2_hp": self.p2_hp,
            "abillity": self.p1Ability,
            "abilityTargetCell": self.p1Target,
            "enemyAbillity": self.p2Ability,
            "enemyAbillityTargetCell": self.p2Target,
            "p1_hit_mine": p1_hit_mine_flag,
            "p2_hit_mine": p2_hit_mine_flag
            ,"collision_event":self.collision_event
        }
        print(f"SENDING TO UNITY: {dataToSend}")
        self.broadcast(dataToSend)
        self.p1Move = None
        self.p2Move = None
        self.p1Ability="NONE"
        self.p2Ability="NONE"
        self.p1Target=-1
        self.p2Target=-1
        time.sleep(4.0)
        if self.p1_hp>0 and self.p2_hp>0:
            self.startNewRound()



    def timer_logic(self):
        was_interupted=self.roundEvent.wait(timeout=self.roundDuration)
        if was_interupted:
            print("BOTH ARE DONE")
        else:
            print("SOMEONE DIDNTMOVE")
            self.handleTimeOut()

    def handleTimeOut(self):
        if self.p1Move==None:
            self.p1_hp = 0
            self.p1Move = -1
            self.p1Target = -1
            self.p1Ability = "NONE"

        if self.p2Move==None:
            self.p2_hp = 0
            self.p2Move = -1
            self.p2Target = -1
            self.p2Ability = "NONE"

        self.resolveTurn()


    def end_round(self):
        self.isRoundActive=False
        if self.p1Move==None:
            self.p1Move=-1

        if self.p2Move==None:
            self.p2Move=-1

        moves={"type":"ROUND MOVES","p1Move":self.p1Move,
                "p2Move":self.p2Move}
        self.broadcast(moves)


    def broadcast(self, data):
        json_data = json.dumps(data)
        try:
            self.player1.send(json_data.encode())
            self.player2.send(json_data.encode())
            print("BRODCASTED!!")
        except:
            print("Error broadcasting message")


    def switchTurns(self):
        if(self.player1Turn==True):
            self.player1Turn=False
            self.player2Turn=True

        else:
            self.player1Turn = True
            self.player2Turn = False

    def playerTurn(self):
        return self.player1 if self.player1Turn == True else self.player2

    def makeMove(self,player, move):
        if player==self.player1 and self.player1Turn==True:
            self.switchTurns()
            return f"player 1 moved to {move }"
        elif player==self.player2 and self.player2Turn==True:
            self.switchTurns()
            return f"player 2 moved to {move }"

    def player1Move(self,move):
        pass

    def player2Move(self,move):
        pass


