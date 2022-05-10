using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BijouDB
{
    public record Record
    {
        public readonly Guid Id;

        public Record() => Id = IncrementalGuid.NextGuid();
    }
}
