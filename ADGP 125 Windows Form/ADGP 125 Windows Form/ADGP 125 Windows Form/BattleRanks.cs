﻿using System;
using System.Collections.Generic;
using System.Linq;
using Inferances;
using FSM;
using Items;
using ADGP_125_Form;
using System.Xml.Serialization;

namespace BattleRanks
{
    [Serializable]
    public enum TurnStates  //States for the Party
    {
        WAIT = 0,   //Party will do nothing while in this state
        USE = 1,    //Party will use an item on the current Unit at this state
        ATTACK = 2, //Party will attack with the current Unit at this state
        END = 3,    //Signals When the party will change current Units.
    }

    [Serializable]
    public class Unit : IStats
    {
        public int maxHealth;   //How much health the Unit can Have.
        int _health;            //how close to not being able to do Actions are you.
        int _attack;            //How much health will be taken away when this Unit attacks another
        int _speed;             //When can this Unit Attack when it is this Unit's Party's trun
        string _name;           //What the console will call to identify this Unit
        public bool inPar;      //Tells if this unit is part of a class or not.
        public double currExp;  //The Current amount of experiance this Unit Has From reaching the next level.
        public double maxExp;   //The amount of experiance needed to reach the next level
        public int level;       //Determines how much to increase the Unit's base stats by

        public Item uitem;      //The Item that the Unit will carry

        public Unit()  //Unit Default Constructor
        {

        }

        public Unit(int a, int h, int s, bool iP, string n, double cEx, double mEx, int l, Item i)  //Unit Constructor
        {
            attack = a;
            health = h;
            maxHealth = h;
            speed = s;
            inPar = iP;
            name = n;
            currExp = cEx;
            maxExp = mEx;
            level = l;
            uitem = i;
        }

        public int health
        {
            get
            {
                return _health;
            }

            set
            {
                _health = value;
            }
        }

        public int attack
        {
            get
            {
                return _attack;
            }

            set
            {
                _attack = value;
            }
        }

        public int speed
        {
            get
            {
                return _speed;
            }

            set
            {
                _speed = value;
            }
        }

        public string name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }


    }

    [Serializable]
    public class Party : IParty    //The Object which will store the units and control their actions.
    {
        [XmlIgnore]
        public static BattleLog BatLog = new BattleLog();
        FSM<TurnStates> _turnHandler = new FSM<TurnStates>(TurnStates.WAIT);      
        List<Unit> _team = new List<Unit>();
        Unit _currUnit;

        Del attack = Attack;

        public List<Unit> team
        {
            get
            {
                return _team;
            }

            set
            {
                _team = value;
            }
        }

        public Unit currUnit
        {
            get
            {
                return _currUnit;
            }

            set
            {
                _currUnit = value;
            }
        }

        public FSM<TurnStates> turnHandler
        {
            get
            {
                return _turnHandler;
            }

            set
            {
                _turnHandler = value;
            }
        }


        public Party()
        {
            turnHandler.AddState(TurnStates.USE);       //Adds States to the Party's FSM
            turnHandler.AddState(TurnStates.ATTACK);
            turnHandler.AddState(TurnStates.END);
            turnHandler.AddTransition(TurnStates.WAIT, TurnStates.USE);     //Adding Valid Transitions between states for the FSM
            turnHandler.AddTransition(TurnStates.USE, TurnStates.ATTACK);
            turnHandler.AddTransition(TurnStates.ATTACK, TurnStates.USE);
            turnHandler.AddTransition(TurnStates.ATTACK, TurnStates.END);
            turnHandler.AddTransition(TurnStates.END, TurnStates.USE);
            turnHandler.AddTransition(TurnStates.END, TurnStates.WAIT);
        }

        public bool AddUnit(Unit u)     //Adds a Unit to a party
        {
            if (u.inPar == true)  //Does this unit already belong to a party?
            {
                BatLog.BB.AppendText("This Unit is already in a Party.");    //If the Unit is in a party.
                return false;
            }

            team.Add(u);    //If the Unit is not in this class.
            u.inPar = true; //Unit is in a party
            team = team.OrderByDescending(o => o.speed).ToList();   //Sorts List by highest to lowest speed
            currUnit = team[0];
            return true;
        }

        public IParty Attack(IParty other) //The Party will attack a Unit of another Party using the stats of this party's current Unit.
        {
            if(team.Count >= 2) //If there are 2 or more pople
            {
                team = team.OrderByDescending(o => o.speed).ToList();   //Sorts List by highest to lowest speed
            }
            
            for (int a = other.team.Count - 1; a >= 0; a--)  //Getting a target
            {
                if(currUnit.health > 0) //
                {
                    if (other.team[a].health > 0)         //Does other have health.
                    {
                        other.team[a].health -= currUnit.attack;
                        if (other.team[a].health > 0)   //If other's health is greater than 0
                        {
                            BatLog.BB.AppendText(currUnit.name + " attacks " + other.team[a].name + ". ");
                            BatLog.BB.AppendText(other.team[a].name + "'s health is now " + other.team[a].health); //Give Results
                        }

                        else    //If other's health is less than or equal to 0
                        {
                            BatLog.BB.AppendText(currUnit.name + " attacks " + other.team[a].name + ". ");
                            BatLog.BB.AppendText(other.team[a].name + " has died!! " + currUnit.name + " has gained 10 experiance. ");  //They died
                            other.team[a].health = 0;   //Sets other's health to 0
                            currUnit.currExp += 10; //Award current Unit Experiance
                            currUnit = this.CheckLevl(currUnit);   //Check Current Unit's Level
                        }
                        break;  //Stop loop
                    }

                    //If Unit targeted has no health, keep searching through the list. larger index to smaller index
                } 
            }
            return other;
        }

        public bool PartyHealth()   //Checks the Health of all Units in the party and tells if they are alive
        {
            foreach(Unit u in team) //For every Unit in team
            {
                if(u.health > 0)    //if Thier health is greater than 0
                {
                    return true;    //Break and return true
                }
            }
            return false;           //No one has health and return false.
        }

        public IParty UseItem(IParty other)   //Party uses Unit's item on the Unit
        {
            if(currUnit.health <= (currUnit.maxHealth / 2) && currUnit.health > 0 && currUnit.uitem.health > 0)
            {
                currUnit.health += currUnit.uitem.health;   //Give Unit Item's Health
                BatLog.BB.AppendText(currUnit.name + " uses " + currUnit.uitem.name + " and has " + currUnit.health + " health. ");
                currUnit.uitem.health = 0;  //Set item's health to 0
            }
            return this;
        }

        public Unit CheckLevl(Unit other) //Checks to see if the Unit can increase its level and obtain the stat increases that comes with it.
        {
            int tempLev = other.level;
            int baseint = 10;       //How much each level will increment by.
            double expCh = 10;      //Changing base change
            double Unused = 0;

            for (int lev = other.level; lev > 1; lev--)
            {
                expCh += baseint;
                Unused = expCh;
            }

            for (expCh = Unused; expCh + baseint < other.currExp; expCh += baseint)     //Extra EXP
            {
                tempLev++;
                if (tempLev % 3 == 0 || tempLev == 0)   // Level is 0 or Divisable by 3
                {
                    other.maxHealth += 10;    //Increase Max Health by 10
                    BatLog.BB.AppendText("And has gained 10 max health. ");
                }

                else if (tempLev % 3 == 1) //if level has a remainder of 1.
                {
                    other.attack += 10;   //Increase Attack by 10
                    BatLog.BB.AppendText("And has gained 10 attack damage. ");
                }

                else if (tempLev % 3 == 2)    //if Level has a remainder of 2
                {
                    other.speed += 10;    //Increase speed by 10
                    BatLog.BB.AppendText("And has gained 10 speed. ");
                }
            }

            other.level = tempLev;
            other.health = other.maxHealth;
            BatLog.BB.AppendText(other.name + " is now at level " + other.level + ". ");
            return other;
        }
    }
}