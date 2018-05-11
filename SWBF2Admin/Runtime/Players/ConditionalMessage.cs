using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SWBF2Admin.Structures;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Runtime.Players
{
    public enum TriggerCondition
    {
        ScoreSinceLastKillGreaterThan,
        ScoreSinceLastDeathGreaterThan,
        KillsSinceLastScoreGreaterThan,
        KillsSinceLastDeathGreaterThan,
        DeathsSinceLastKillGreaterThan,
        DeathsSinceLastScoreGreaterThan,
        TotalKillsGreatherThan,
        TotalScoreGreaterThan,
        TotalDeathsGreaterThan
    }

    public enum ResetCondition
    {
        OnScore,
        OnKill,
        OnDeath,
        OnSlotReset
    }

    public class ConditionalMessage
    {
        [XmlAttribute]
        public TriggerCondition Trigger { get; set; }
        [XmlAttribute]
        public ResetCondition Reset { get; set; }
        [XmlAttribute]
        public int Threshold { get; set; }

        public List<string> MessagePool { get; set; }

        public void TickPlayer(Player p, AdminCore core)
        {
            //reset triggers
            bool rs = false;
            switch (Reset)
            {
                case ResetCondition.OnScore: rs = p.HasScored; break;
                case ResetCondition.OnKill: rs = p.HasKilled; break;
                case ResetCondition.OnDeath: rs = p.HasDied; break;
            }

            if (p.HasSlotReset) rs = true;
            if (rs) p.MessageStates[this] = false;

            //exit if triggered but not reset yet
            if (p.MessageStates[this]) return;

            bool tr = false;
            switch (Trigger)
            {
                case TriggerCondition.ScoreSinceLastKillGreaterThan: tr = (p.ScoreSinceLastKill > Threshold); break;
                case TriggerCondition.ScoreSinceLastDeathGreaterThan: tr = (p.ScoreSinceLastDeath > Threshold); break;

                case TriggerCondition.KillsSinceLastScoreGreaterThan: tr = (p.KillsSinceLastScore > Threshold); break;
                case TriggerCondition.KillsSinceLastDeathGreaterThan: tr = (p.KillsSinceLastDeath > Threshold); break;

                case TriggerCondition.DeathsSinceLastKillGreaterThan: tr = (p.DeathsSinceLastKill > Threshold); break;
                case TriggerCondition.DeathsSinceLastScoreGreaterThan: tr = (p.DeathsSinceLastScore > Threshold); break;

                case TriggerCondition.TotalKillsGreatherThan: tr = (p.Kills > Threshold); break;
                case TriggerCondition.TotalScoreGreaterThan: tr = (p.Score > Threshold); break;
                case TriggerCondition.TotalDeathsGreaterThan: tr = (p.Deaths > Threshold); break;
            }

            if (tr)
            {
                core.Rcon.Say(Util.FormatString(SelectMessage(),
                    "{player}", p.Name, "{value}", (Threshold - 1).ToString()
                    ));
                p.MessageStates[this] = true;
            }
        }
        private string SelectMessage()
        {
            return MessagePool[new Random().Next(0, MessagePool.Count - 1)];
        }
    }
}