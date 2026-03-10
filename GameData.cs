using System;

[Serializable]
public class GameData
{
    public string type;
    public string content;
    public int symbol;
    public int time;
    public int index;
    public int p1Move;
    public int p2Move;
    public int p1_hp;
    public int p2_hp;
    public String abillity;
    public int abilityTargetCell;
    public String enemyAbillity;
    public int enemyAbillityTargetCell;
    public bool p1_hit_mine;
    public bool p2_hit_mine;
    public bool collision_event;

    public GameData(string type, string content)
    {
        this.type = type;
        this.content = content;
        this.symbol = 0;
        this.abillity = null;
        this.abilityTargetCell = -1;
        this.p1_hp = 3;
        this.p2_hp = 3;
    }
}