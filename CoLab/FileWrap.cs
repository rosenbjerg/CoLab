﻿using System;

namespace CoLab
{
    public class FileWrap
    {
        public FileWrap()
        {
        }

        public FileWrap(string name)
        {
            Name = name;
            Id = Guid.NewGuid().ToString("N");
        }

        public string Name { get; set; }
        public string Id { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}