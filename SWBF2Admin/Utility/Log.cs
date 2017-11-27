namespace SWBF2Admin.Utility
{
    class Log
    {
        public const string CORE_PREFIX = "[COR] ";
        public const string CORE_START = CORE_PREFIX + "Starting {0} v{1} {2} ..."; //product -name, -version, -author
        public const string CORE_READ_CONFIG = CORE_PREFIX + "Reading configuration...";
        public const string CORE_READ_CONFIG_OK = CORE_PREFIX + "Config OK.";

        public const string FILE_PREFIX = "[IO ] ";
        public const string FILE_DIRECTORY_CREATE = FILE_PREFIX + "Created directory '{0}'"; //directory path
        public const string FILE_DIRECTORY_CREATE_ERROR = FILE_PREFIX + " Failed to create directory '{0}' ({1})"; //directory path, error string
        public const string FILE_XML_PARSE = FILE_PREFIX + "Parsed XML-file '{0}', type: {1}"; //file path, class name
        public const string FILE_XML_PARSE_ERROR = FILE_PREFIX + " Error reading XML-file '{0}', type: {1} ({2})"; //file path, class name, error message
        public const string FILE_XML_CREATE = FILE_PREFIX + "Wrote XML-file '{0}', type: {1}"; //file path, class name, error message
        public const string FILE_XML_CREATE_ERROR = FILE_PREFIX + "Error writing XML-file '{0}', type: {1} ({2})"; //file path, class name, error message
        public const string FILE_DEFAULT_UNPACK = FILE_PREFIX + "Unpacked default file '{0}'"; //file path
        public const string FILE_DEFAULT_UNPACK_ERROR = FILE_PREFIX + "Failed to unpack default file '{0}' (1)"; //file path, error message

        public const string SQL_PREFIX = "[SQL] ";
        public const string SQL_ALREADY_OPEN = SQL_PREFIX + "Database connection already open";
        public const string SQL_OPEN = SQL_PREFIX + "Opening database connection...";
        public const string SQL_OPEN_OK = SQL_PREFIX + "Database OK.";
        public const string SQL_OPEN_ERROR = SQL_PREFIX + "Couldn't open database : {0}"; //error message
        public const string SQL_CLOSE = SQL_PREFIX + "Closing Database..."; //error message
        public const string SQL_QUERY = SQL_PREFIX + "Query: {0}"; //query string
        public const string SQL_QUERY_ERRPR = SQL_PREFIX + "Query failed : {0}"; //error string
        public const string SQL_NONQUERY = SQL_PREFIX + "NonQuery: {0}"; //query string
        public const string SQL_NONQUERY_ERROR = SQL_PREFIX + "Query failed : {0}"; //error string
        public const string SQL_PARAMETER_NOVALUE = SQL_PREFIX + "No value for parameter '{0}' specified"; //parameter name

        public const string WEB_PREFIX = "[WEB] ";
        public const string WEB_START = WEB_PREFIX + "WebAdmin started at '{0}'"; //root url
        public const string WEB_STOP = WEB_PREFIX + "WebAdmin stopped.";
        public const string WEB_REQUEST = WEB_PREFIX + "Got request '{0}'"; //uri
        public const string WEB_NO_PAGE_HANDLER = WEB_PREFIX + "No page handler matching '{0}' found. Returning 404"; //path
        public const string WEB_UNHANDLED_ERROR = WEB_PREFIX + "WebAdmin error({0})"; //error message
        public const string WEB_SERVE_STATIC = WEB_PREFIX + "Serving static file '{0}'"; //file name
        public const string WEB_STATIC_NOT_FOUND = WEB_PREFIX + "Static file '{0}' not found. Returning 404."; //file name
        public const string WEB_REPLACEMENT_NOVALUE = WEB_PREFIX + "No value for replacement '{0}' specified";
    }
}
