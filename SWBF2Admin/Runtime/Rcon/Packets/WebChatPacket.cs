/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see<http://www.gnu.org/licenses/>.
 */
using SWBF2Admin.Structures;
using SWBF2Admin.Web.Pages;

namespace SWBF2Admin.Runtime.Rcon.Packets
{
    class WebChatPacket : RconPacket
    {
        public virtual ChatPage.ChatSession Session { get; }

        public WebChatPacket(string command, ChatPage.ChatSession session) : base(command)
        {
            Session = session;
        }

        public ChatMessage[] GetMessages()
        {
            string[] rows = Response.Split('\n');
            ChatMessage[] messages = new ChatMessage[rows.Length];
            for (int i = 0; i < messages.Length; i++)
                messages[i] = new ChatMessage(rows[i]);
            return messages;
        }
    }
}