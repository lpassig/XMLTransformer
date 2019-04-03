using System.Collections.Generic;
using System.Linq;

namespace XMLTransformer
{
    public class LogMessageReader
    {
        private MessageStack _messageStack;
        public LogMessageReader(MessageStack messageStack)
        {
            _messageStack = messageStack;
        }


        /**
            Liefert alle relevanten Log Messages
        */
        public IEnumerable<MessageType> GetLogs()
        {
            return _messageStack.Messages.Where(
                        x => x.Grouping != null
                        && x.Code != 15082
                        //&& (x.Grouping.Item.ToString().StartsWith("towercrane") || x.Grouping.Item.ToString().StartsWith("event"))
                    ).ToArray();

        }

        /**
            Liefert die Konzerweite eindeutige ID der Maschine
         */
        public string GetCraneId()
        {
            var origin = _messageStack.Item as OriginType;
            return origin.Machine.Liuid;
        }
    }
}