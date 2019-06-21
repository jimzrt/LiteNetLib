using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibSample
{
    class Player
    {
        public int Id { get; }

        public string Name { get; set; }

        public Player(int id)
        {
            Id = id;
            Name = id.ToString();
        }
        public Player(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
