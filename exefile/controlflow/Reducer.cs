using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.util;
using NLog;

namespace exefile.controlflow
{
    public class Reducer
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public readonly SortedDictionary<uint, IBlock> blocks = new SortedDictionary<uint, IBlock>();

        public Reducer(ControlFlowProcessor processor)
        {
            foreach (var block in processor.blocks)
            {
                blocks.Add(block.Key, block.Value);
            }
        }

        public void reduce()
        {
            var reduced = false;
            do
            {
                reduced = blocks.Values.Reverse().Any(reduceIf);
            } while (reduced);
        }

        private bool reduceIf(IBlock condition)
        {
            /*
            if(condition<exit=conditional>) body<exit=unconditional>; commonCode;
            */
            
            if (condition.exitType != ExitType.Conditional)
                return false;

            var common = condition.trueExit;
            Debug.Assert(common != null);
            var body = condition.falseExit;
            Debug.Assert(body != null);
            
            if (body.exitType == ExitType.Unconditional && body.trueExit == common)
            {
                logger.Debug($"Reduce: condition={condition.start:X} body={body.start:X} common={common.start:X}");
                
                var compound = new IfBlock(condition, body, common, true);
                blocks.Remove(condition.start);
                blocks.Remove(body.start);
                blocks.Add(compound.start, compound);
                return true;
            }
            
            // swap and try again
            {
                var tmp = common;
                common = body;
                body = tmp;
            }
            
            if (body.exitType == ExitType.Unconditional && body.trueExit == common)
            {
                logger.Debug($"Reduce: condition={condition.start:X} body={body.start:X} common={common.start:X}");

                var compound = new IfBlock(condition, body, common, false);
                blocks.Remove(condition.start);
                blocks.Remove(body.start);
                blocks.Add(compound.start, compound);
                return true;
            }

            return false;
        }
        
        public void dump(IndentedTextWriter writer)
        {
            foreach (var block in blocks.Values)
            {
                block.dump(writer);
                writer.WriteLine();
            }
        }
    }
}
