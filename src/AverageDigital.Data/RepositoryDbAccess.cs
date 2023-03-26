using AverageDigital.Core.Data;
using AverageDigital.Core.ExceptionHandling;
using AverageDigital.Core.Threading;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;


namespace AverageDigital.Data
{
    public class RepositoryDbAccess
    {
        private const int TOO_LONG_COMMANDTIMEOUT = 14400;

        private object _parameters;
        private Dictionary<string, DbType> _output;
        private CommandType _commandType = CommandType.StoredProcedure;
        private bool _useLongTimeout = false;
        private bool _buffered = true;
        private string _connectionString;
        private int _customTimeout = -1;
        private DynamicParameters _outputParameters;
        private bool? _logEnabled = null;

        public static IConfiguration Configuration { get; set; }

        private readonly string _defaultConnectionString;
        private readonly bool _defaultLogEnabled = false;
        private ILogger logger;

        public IDbConnectionFactory DbConnectionFactory { get; set; }

        public RepositoryDbAccess()
        {
            if(Configuration == null)
                throw new InvalidOperationException("Configuration is null");

            var connectionStringName = Configuration["AverageDigital:DefaultConnectionString"];
            var connectionString = Configuration[$"ConnectionStrings:{connectionStringName}"];
            var log = Configuration["AverageDigital:Log"];

            this._defaultLogEnabled = log == "true";

            if (connectionString != null)
                _defaultConnectionString = connectionString;
        }

        public string GetConnectionString(string name)
        {
            var connection = Configuration[$"ConnectionStrings:{name}"];

            return connection ?? _defaultConnectionString;
        }

        public RepositoryDbAccess UseConnection(string connectionString)
        {
            Throw.IfArgumentNullOrEmpty(connectionString, nameof(connectionString));

            _connectionString = connectionString;
            return this;
        }

        public RepositoryDbAccess UseConnectionString(string connectionStringName)
        {
            Throw.IfArgumentNullOrEmpty(connectionStringName, nameof(connectionStringName));

            var connection = Configuration[$"ConnectionStrings:{connectionStringName}"];

            if (connection == null)
                throw new NullReferenceException($"No connection string with name \"{connectionStringName}\" was found");

            _connectionString = connection;

            return this;
        }

        public RepositoryDbAccess WithLog()
        {
            _logEnabled = true;
            return this;
        }

        public RepositoryDbAccess AsProcedure()
        {
            _commandType = CommandType.StoredProcedure;
            return this;
        }

        public RepositoryDbAccess AsStatement()
        {
            _commandType = CommandType.Text;
            return this;
        }

        public RepositoryDbAccess WithParameters(object parameters)
        {
            _parameters = parameters;
            return this;
        }

        public RepositoryDbAccessWithOutput WithOutput(string name, DbType type)
        {
            _output ??= new Dictionary<string, DbType>();

            if (_output.ContainsKey(name))
                _output[name] = type;
            else
                _output.Add(name, type);

            return new RepositoryDbAccessWithOutput(this);
        }

        public RepositoryDbAccess UseLongTimeout()
        {
            _useLongTimeout = true;
            return this;
        }

        public RepositoryDbAccess WithCustomTimeout(int timeout)
        {
            if (timeout <= 0) throw new InvalidOperationException("Invalid timeout.");
            _customTimeout = timeout;
            return this;
        }

        public RepositoryDbAccess IsNotBuffered()
        {
            _buffered = false;
            return this;
        }

        private void Log(string message)
        {
            if(logger != null)
            {
                logger.LogInformation(message);
                return;
            }

            var shouldLog = _logEnabled ?? _defaultLogEnabled;

            if (!shouldLog) return;

            var date = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine($"[{date}] [AverageDigital.Data] - Executing '{message}'");
        }

        private IDbConnection GetConnection()
        {
            IDbConnection connection;

            if (ScopeIsActive)
            {
                var transaction = ThreadStorage.GetData<IDbTransaction>(TransactionScope.ScopeTransactionKey);

                if (transaction != null && transaction.Connection != null)
                    connection = transaction.Connection;
                else
                {
                    connection = this.GetDbConnection();

                    connection.Open();

                    transaction = connection.BeginTransaction();
                    ThreadStorage.SetData(TransactionScope.ScopeTransactionKey, transaction);
                }
            }
            else
            {
                connection = this.GetDbConnection();

                connection.Open();
            }

            return connection;
        }

        private IDbConnection GetDbConnection()
        {
            Throw.IfReferenceNull(this.DbConnectionFactory, "DbConnectionFactory");

            _connectionString ??= _defaultConnectionString;

            return this.DbConnectionFactory.GetDbConnection(_connectionString, this);
        }

        private bool ScopeIsActive => ThreadStorage.GetData<bool>(TransactionScope.ActiveScopeKey);

        private IDbTransaction ActiveTransaction
        {
            get
            {
                return !ScopeIsActive ? null : ThreadStorage.GetData<IDbTransaction>(TransactionScope.ScopeTransactionKey);
            }
        }

        private object GetParameters()
        {
            if (!_output.HasItems()) return _parameters;

            var parameters = new DynamicParameters();
            parameters.AddDynamicParams(_parameters);
            foreach (var item in _output)
            {
                parameters.Add(item.Key, null, item.Value, ParameterDirection.Output);
            }

            _outputParameters = parameters;
            return parameters;
        }

        internal DynamicParameters GetOutputParameters()
        {
            return _outputParameters;
        }

        #region List

        public IEnumerable<dynamic> List(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            IEnumerable<dynamic> result;

            var connection = this.GetConnection();

            try
            {
                Log(sql);
                result = connection.Query(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    buffered: _buffered,
                    transaction: this.ActiveTransaction);
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }

            return result;
        }

        public IEnumerable<T> List<T>(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            IEnumerable<T> result;

            var connection = this.GetConnection();

            try
            {
                Log(sql);
                result = connection.Query<T>(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    buffered: _buffered,
                    transaction: this.ActiveTransaction);
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }

            return result;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>> List<T1, T2>(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            IEnumerable<T1> result1;
            IEnumerable<T2> result2;

            var connection = this.GetConnection();

            try
            {
                Log(sql);
                var reader = connection.QueryMultiple(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    transaction: this.ActiveTransaction);

                result1 = reader.Read<T1>(_buffered);
                result2 = reader.Read<T2>(_buffered);
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>>(result1, result2);
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> List<T1, T2, T3>(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            IEnumerable<T1> result1;
            IEnumerable<T2> result2;
            IEnumerable<T3> result3;

            var connection = this.GetConnection();

            try
            {
                Log(sql);
                var reader = connection.QueryMultiple(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    transaction: this.ActiveTransaction);

                result1 = reader.Read<T1>(_buffered);
                result2 = reader.Read<T2>(_buffered);
                result3 = reader.Read<T3>(_buffered);
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>(result1, result2, result3);
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> List<T1, T2, T3, T4>(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            IEnumerable<T1> result1;
            IEnumerable<T2> result2;
            IEnumerable<T3> result3;
            IEnumerable<T4> result4;

            var connection = this.GetConnection();

            try
            {
                Log(sql);
                var reader = connection.QueryMultiple(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    transaction: this.ActiveTransaction);

                result1 = reader.Read<T1>(_buffered);
                result2 = reader.Read<T2>(_buffered);
                result3 = reader.Read<T3>(_buffered);
                result4 = reader.Read<T4>(_buffered);
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>(result1, result2, result3, result4);
        }

        #endregion

        #region List Async

        public async Task<IEnumerable<T>> ListAsync<T>(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            IEnumerable<T> result;
            var connection = this.GetConnection();

            try
            {
                Log(sql);
                result = await connection.QueryAsync<T>(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    transaction: this.ActiveTransaction);
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }

            return result;
        }

        #endregion

        #region Get

        public T Get<T>(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            T result;
            var connection = this.GetConnection();

            try
            {
                Log(sql);
                result = connection.Query<T>(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    buffered: false,
                    transaction: this.ActiveTransaction).FirstOrDefault();
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }

            return result;
        }

        #endregion

        #region Get Async

        public async Task<T> GetAsync<T>(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            T result;
            var connection = this.GetConnection();

            try
            {
                Log(sql);
                var queryResult = await connection.QueryAsync<T>(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    transaction: this.ActiveTransaction);
                result = queryResult.FirstOrDefault();
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }

            return result;
        }

        #endregion

        #region Execute

        public T Execute<T>(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            var connection = GetConnection();

            try
            {
                Log(sql);
                return connection.Query<T>(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    buffered: false,
                    transaction: this.ActiveTransaction).FirstOrDefault();
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }
        }

        public void Execute(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            var connection = GetConnection();

            try
            {
                Log(sql);
                connection.Execute(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    transaction: this.ActiveTransaction);
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }
        }

        #endregion

        #region Execute Async

        public async Task<T> ExecuteAsync<T>(string sql)
        {
            Throw.IfArgumentNullOrEmpty(sql, nameof(sql));

            var connection = GetConnection();

            try
            {
                Log(sql);
                var queryResult = await connection.QueryAsync<T>(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    transaction: this.ActiveTransaction);

                return queryResult.FirstOrDefault();
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }
        }

        public async Task ExecuteAsync(string sql)
        {
            var connection = GetConnection();

            try
            {
                await connection.ExecuteAsync(sql, GetParameters(),
                    commandTimeout: _customTimeout > 0
                        ? _customTimeout
                        : (_useLongTimeout ? TOO_LONG_COMMANDTIMEOUT : (int?)null),
                    commandType: _commandType,
                    transaction: this.ActiveTransaction);
            }
            finally
            {
                if (!ScopeIsActive)
                    connection.Dispose();
            }
        }

        #endregion

    }
}
