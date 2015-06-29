﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meticumedia.Classes
{
    public class EzTvEpisode
    {
        #region Properties

        public string Title { get; set; }

        public int Season { get; set; }

        public int Episode { get; set; }

        public int Episode2 { get; set; }

        public List<string> Mirrors { get; set; }

        public string Magnet { get; set; }

        #endregion

        public EzTvEpisode()
        {
            this.Mirrors = new List<string>();
        }

        public EzTvEpisode(EzTvEpisode clone)
        {
            this.Season = clone.Season;
            this.Episode = clone.Episode;
            this.Episode2 = clone.Episode2;
            this.Mirrors = new List<string>();
            foreach (string mirror in clone.Mirrors)
                this.Mirrors.Add(mirror);
            this.Magnet = clone.Magnet;
        }
    }
}