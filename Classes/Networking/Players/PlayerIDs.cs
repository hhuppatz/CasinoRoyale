using System.Collections.Generic;

namespace CasinoRoyale.Classes.Networking.Players;

public class PlayerIDs
    {
        private readonly uint MAX_PLAYERS;
        private readonly Dictionary<uint, bool> ids;

        public PlayerIDs(uint MAX_PLAYERS)
        {
            this.MAX_PLAYERS = MAX_PLAYERS;
            ids = [];
            for (uint ic = 0; ic < MAX_PLAYERS; ic++)
            {
                ids.Add(ic, false);
            }
        }

        public bool RoomForNextPlayer()
        {
            if (ids[MAX_PLAYERS - 1])
            {
                return false;
            }
            return true;
        }

        public uint GetNextID()
        {
            for (uint id = 0; id < ids.Count; id++)
            {
                if (!IsTaken(id))
                {
                    ids[id] = true;
                    return id;
                }
            }
            // if we reach here we have a big problemo
            return 0;
        }

        public void ReleaseID(uint id)
        {
            if (IsTaken(id))
            {
                ids[id] = false;
            }
        }

        private bool IsTaken(uint id)
        {
            return ids[id];
        }
    }