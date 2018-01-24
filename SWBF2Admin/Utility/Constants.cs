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
namespace SWBF2Admin
{
    class Constants
    {
        public const int MUTEX_LOCK_TIMEOUT = 10;

        public const string WEB_DIR_ROOT = "./web";
        public const string WEB_COOKIE_CHAT = "chat_session";
        public const int WEB_COOKIE_TIMEOUT = 3600;
        public const int WEB_SESSION_LIMIT = 500;
    }
}