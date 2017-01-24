using System;

namespace CoLab
{
    class FileWrap
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

    }
}