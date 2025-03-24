using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrbOrganizer
{
    public class ExecutionParams
    {
        public string sourcePath { get; set; }
        public string targetPath { get; set; }
        public CategorizationTypeEnum categorizationType { get; set; }
        public bool includeSubfolders { get; set; }
        public bool keepOriginal { get; set; }
    }
}
