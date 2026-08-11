/**
 * Prism.js 1.29.0 - Minimal Build for MdReader
 * Includes: markup, css, clike, javascript, typescript, csharp, python, json, bash, sql, markdown
 * All languages self-contained, no CDN dependency.
 */
var Prism = (function() {
	var lang = /\blang(?:uage)?-([\w-]+)\b/i;
	var uniqueId = 0;
	var _ = {
		util: {
			encode: function(tokens) {
				if (tokens instanceof Token) {
					return new Token(tokens.type, _.util.encode(tokens.content), tokens.alias);
				} else if (Array.isArray(tokens)) {
					return tokens.map(_.util.encode);
				} else {
					return tokens.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/\u00a0/g, ' ');
				}
			},
			type: function(o) {
				return Object.prototype.toString.call(o).slice(8, -1);
			},
			objId: function(obj) {
				if (!obj['__id']) { Object.defineProperty(obj, '__id', { value: ++uniqueId }); }
				return obj['__id'];
			},
			clone: function(o, visited) {
				var clone, id, type = _.util.type(o);
				visited = visited || {};
				switch (type) {
					case 'Object':
						id = _.util.objId(o);
						if (visited[id]) { return visited[id]; }
						clone = {};
						visited[id] = clone;
						for (var key in o) {
							if (o.hasOwnProperty(key)) { clone[key] = _.util.clone(o[key], visited); }
						}
						return clone;
					case 'Array':
						id = _.util.objId(o);
						if (visited[id]) { return visited[id]; }
						clone = [];
						visited[id] = clone;
						o.forEach(function(v, i) { clone[i] = _.util.clone(v, visited); });
						return clone;
				}
				return o;
			},
			getLanguage: function(element) {
				while (element && !lang.test(element.className)) {
					element = element.parentElement;
				}
				if (element) {
					return (element.className.match(lang) || [, 'none'])[1].toLowerCase();
				}
				return 'none';
			},
			currentScript: function() {
				return null;
			},
			isActive: function(element, className, defaultActivation) {
				var no = 'no-' + className;
				return !(element.classList.contains(no) || !element.classList.contains(className) && defaultActivation);
			}
		},
		languages: {
			'none': {}
		},
		plugins: {},
		highlightAll: function(async, callback) {},
		highlightElement: function(element, async, callback) {},
		highlight: function(text, grammar, language) {
			var env = {
				code: text,
				grammar: grammar,
				language: language
			};
			_.hooks.run('before-tokenize', env);
			if (!env.grammar) {
				throw new Error('The language "' + env.language + '" is not supported.');
			}
			env.tokens = _.tokenize(env.code, env.grammar);
			_.hooks.run('after-tokenize', env);
			return Token.stringify(_.util.encode(env.tokens), env.language);
		},
		tokenize: function(text, grammar) {
			var strarr = [text];
			var rest = grammar.rest;
			if (rest) {
				for (var token in rest) {
					grammar[token] = rest[token];
				}
				delete grammar.rest;
			}
			tokenloop: for (var token in grammar) {
				if (!grammar.hasOwnProperty(token) || !grammar[token]) { continue; }
				var patterns = grammar[token];
				patterns = Array.isArray(patterns) ? patterns : [patterns];
				for (var j = 0; j < patterns.length; ++j) {
					var pattern = patterns[j],
						inside = pattern.inside,
						lookbehind = !!pattern.lookbehind,
						greedy = !!pattern.greedy,
						lookbehindLength = 0,
						alias = pattern.alias;
					pattern = pattern.pattern || pattern;
					for (var i = 0; i < strarr.length; i++) {
						var str = strarr[i];
						if (strarr.length > text.length) { break tokenloop; }
						if (str instanceof Token) { continue; }
						var removeCount = 1;
						var match = pattern.exec(str);
						if (!match) { continue; }
						if (lookbehind) {
							lookbehindLength = match[1].length;
							match.index += match[1].length;
							match[0] = match[0].slice(match[1].length);
						}
						var from = match.index,
							matchStr = match[0],
							before = str.slice(0, from),
							after = str.slice(from + matchStr.length);
						var args = [i, 1];
						if (before) { args.push(before); }
						var wrapped = new Token(token, inside ? _.tokenize(matchStr, inside) : matchStr, alias);
						args.push(wrapped);
						if (after) { args.push(after); }
						Array.prototype.splice.apply(strarr, args);
					}
				}
			}
			return strarr;
		},
		hooks: {
			all: {},
			add: function(name, callback) {
				_.hooks.all[name] = _.hooks.all[name] || [];
				_.hooks.all[name].push(callback);
			},
			run: function(name, env) {
				var callbacks = _.hooks.all[name];
				if (!callbacks || !callbacks.length) { return; }
				for (var i = 0, callback; callback = callbacks[i++];) {
					callback(env);
				}
			}
		},
		Token: Token
	};

	function Token(type, content, alias) {
		this.type = type;
		this.content = content;
		this.alias = alias;
	}

	Token.stringify = function(o, language) {
		if (typeof o == 'string') { return o; }
		if (Array.isArray(o)) {
			return o.map(function(element) {
				return Token.stringify(element, language);
			}).join('');
		}
		var env = {
			type: o.type,
			content: Token.stringify(o.content, language),
			tag: 'span',
			classes: ['token', o.type],
			attributes: {},
			language: language
		};
		var aliases = o.alias;
		if (aliases) {
			if (Array.isArray(aliases)) {
				Array.prototype.push.apply(env.classes, aliases);
			} else {
				env.classes.push(aliases);
			}
		}
		_.hooks.run('wrap', env);
		var attributes = '';
		for (var name in env.attributes) {
			attributes += ' ' + name + '="' + (env.attributes[name] || '').replace(/"/g, '&quot;') + '"';
		}
		return '<' + env.tag + ' class="' + env.classes.join(' ') + '"' + attributes + '>' + env.content + '</' + env.tag + '>';
	};

	// ============================================================
	// LANGUAGE: Markup (HTML/XML)
	// ============================================================
	_.languages.markup = {
		comment: /<!--[\s\S]*?-->/,
		prolog: /<\?[\s\S]+?\?>/,
		doctype: /<!DOCTYPE[\s\S]+?>/i,
		cdata: /<!\[CDATA\[[\s\S]*?]]>/i,
		tag: {
			pattern: /<\/?(?!\d)[^\s>\/=$<%]+(?:\s(?:\s*[^\s>\/=]+(?:\s*=\s*(?:"[^"]*"|'[^']*'|[^\s'">=]+))?)*)?\s*\/?>/i,
			greedy: true,
			inside: {
				'tag': {
					pattern: /^<\/?[^\s>\/]+/i,
					inside: {
						'punctuation': /^<\/?/,
						'namespace': /^[^>\s]+:/
					}
				},
				'attr-value': {
					pattern: /=\s*(?:"[^"]*"|'[^']*'|[^\s'">=]+)/i,
					inside: {
						'punctuation': [/^=/, { pattern: /^(\s*)["']|["']$/, lookbehind: true }]
					}
				},
				'punctuation': /\/?>/,
				'attr-name': {
					pattern: /[^\s>\/]+/,
					inside: {
						'namespace': /^[^>\s]+:/
					}
				}
			}
		},
		entity: /&#?[\da-z]{1,8};/i
	};

	// ============================================================
	// LANGUAGE: CSS
	// ============================================================
	_.languages.css = {
		comment: /\/\*[\s\S]*?\*\//,
		atrule: {
			pattern: /@[\w-]+?.*?(?:;|(?=\s*\{))/i,
			inside: { 'rule': /@[\w-]+/ }
		},
		url: /url\((?:(["'])(\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1|.*?)\)/i,
		selector: /[^{}\s][^{};]*?(?=\s*\{)/,
		{
			pattern: /(\bselector\s*\()([^)]+)(\))/,
			lookbehind: true
		},
		string: {
			pattern: /("|')(?:\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1/,
			greedy: true
		},
		property: /[-_a-z\xA0-\uFFFF][-\w\xA0-\uFFFF]*(?=\s*:)/i,
		important: /!important\b/i,
		function: /[-a-z0-9]+(?=\()/i,
		punctuation: /[(){};:,]/
	};

	_.languages.css.inside = _.languages.markup.inside;
	_.languages.css.atrule.inside.rest = _.languages.css;

	// ============================================================
	// LANGUAGE: C-like (base for many languages)
	// ============================================================
	_.languages.clike = {
		comment: [
			{ pattern: /(^|[^\\])\/\*[\s\S]*?(?:\*\/|$)/, lookbehind: true, greedy: true },
			{ pattern: /(^|[^\\:])\/\/.*/, lookbehind: true, greedy: true }
		],
		string: {
			pattern: /(["'])(?:\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1/,
			greedy: true
		},
		'class-name': {
			pattern: /(\b(?:class|interface|extends|implements|trait|instanceof|new)\s+|\bcatch\s+\()[\w.\\]+/i,
			lookbehind: true,
			inside: { punctuation: /[.\\]/ }
		},
		keyword: /\b(?:if|else|while|do|for|return|in|instanceof|function|new|try|throw|catch|finally|null|break|continue|case|switch|default|yield|var|let|const|typeof|void|delete)\b/,
		boolean: /\b(?:true|false)\b/,
		function: /\b\w+(?=\()/,
		number: /\b0x[\da-f]+\b|(?:\b\d+\.?\d*|\B\.\d+)(?:e[+-]?\d+)?/i,
		operator: /[<>]=?|[!=]=?=?|--?|\+\+?|&&?|\|\|?|[?*/~^%]/,
		punctuation: /[{}[\];(),.:]/
	};

	// ============================================================
	// LANGUAGE: JavaScript
	// ============================================================
	_.languages.javascript = _.languages.extend('clike', {
		'class-name': [
			_.languages.clike['class-name'],
			{
				pattern: /(\b(?:class|interface|extends|implements|instanceof|new)\s+)[\w.\\]+/,
				lookbehind: true
			}
		],
		keyword: [
			{ pattern: /((?:^|})\s*)(?:catch|finally)\b/, lookbehind: true },
			{
				pattern: /(^|[^.]|\.\.\.\s*)\b(?:as|async(?=\s*(?:function\b|\(|[$\w]|$))|await|break|case|class|const|continue|debugger|default|delete|do|else|enum|export|extends|for|from|function|get|if|implements|import|in|instanceof|interface|let|new|null|of|package|private|protected|public|return|set|static|super|switch|this|throw|try|typeof|undefined|var|void|while|with|yield)\b/,
				lookbehind: true
			}
		],
		'#number': /\b(?:(?:0[xX](?:[\dA-Fa-f](?:_[\dA-Fa-f])?)+|0[bB](?:[01](?:_[01])?)+|0[oO](?:[0-7](?:_[0-7])?)+)n?|(?:\d(?:_\d)?)+n|NaN|Infinity)\b|(?:\b(?:\d(?:_\d)?)+\.?(?:\d(?:_\d)?)*(?:[eE][+-]?(?:\d(?:_\d)?)+)?|\B\.\d+(?:[eE][+-]?(?:\d(?:_\d)?)+)?)/,
		function: /#?(?!\d)[\w$]+(?=\s*(?:\.\s*(?:apply|bind|call)?\s*)?\()/,
		operator: /--|\+\+|\*\*=?|=>|&&=?|\|\|=?|[!=]==|<<=?|>>>?=?|[-+*/%&|^!=<>]=?|\.{3}|\?\?=?|\?\.?|[~:]/
	});
	_.languages.javascript['class-name'][0].inside = _.languages.javascript;
	_.languages.js = _.languages.javascript;

	// ============================================================
	// LANGUAGE: TypeScript
	// ============================================================
	_.languages.typescript = _.languages.extend('javascript', {
		'class-name': {
			pattern: /(\b(?:class|interface|extends|implements|instanceof|new|type|enum)\s+)[\w.\\]+/,
			lookbehind: true
		},
		keyword: [
			/\b(?:abstract|as|asserts|async|await|break|case|catch|class|const|constructor|continue|debugger|declare|default|delete|do|else|enum|export|extends|finally|for|from|function|get|if|implements|import|in|instanceof|interface|is|keyof|let|module|namespace|new|null|of|override|package|private|protected|public|readonly|return|require|set|static|super|switch|this|throw|try|type|typeof|undefined|var|void|while|with|yield)\b/
		],
		'builtin': /\b(?:string|Function|any|number|boolean|Array|symbol|console|Promise|unknown|never)\b/
	});
	_.languages.ts = _.languages.typescript;

	// ============================================================
	// LANGUAGE: C#
	// ============================================================
	_.languages.csharp = _.languages.extend('clike', {
		keyword: /\b(?:abstract|add|alias|as|ascending|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|descending|do|double|dynamic|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|from|get|global|goto|group|if|implicit|in|int|interface|internal|into|is|join|let|lock|long|namespace|new|null|object|operator|orderby|out|override|params|partial|private|protected|public|readonly|ref|remove|return|sbyte|sealed|select|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|value|var|virtual|void|volatile|where|while|yield)\b/,
		string: [
			{
				pattern: @"/(?:\\.|[^\\"\r\n])*?""/,
				greedy: true
			},
			{
				pattern: /@"(?:""|\\[\s\S]|[^\\"])*"(?!")/,
				greedy: true
			},
			{
				pattern: /(["'])(?:\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1/,
				greedy: true
			}
		],
		'class-name': [
			{
				pattern: /\b[A-Z]\w*(?:\.\w+)*\b(?=\s+\w+)/,
				inside: { punctuation: /\./ }
			},
			{
				pattern: /(\[)[A-Z]\w*(?:\.\w+)*\b/,
				lookbehind: true,
				inside: { punctuation: /\./ }
			},
			{
				pattern: /(\b(?:class|interface)\s+[A-Z]\w*(?:\.\w+)*\s*:\s*)[A-Z]\w*(?:\.\w+)*\b/,
				lookbehind: true,
				inside: { punctuation: /\./ }
			}
		],
		number: /\b0x[\da-f]+\b|(?:\b\d+\.?\d*|\B\.\d+)f?/i,
		operator: /[<>]=?|[!=]=?=?|--?|\+\+?|&&?|\|\|?|[?*/~^%]/,
		punctuation: /[{}[\];(),.:]/
	});

	// ============================================================
	// LANGUAGE: Python
	// ============================================================
	_.languages.python = {
		comment: {
			pattern: /(^|[^\\])#.*/,
			lookbehind: true
		},
		'string-interpolation': {
			pattern: /(?:f|rf|fr)(?:("""|''')[\s\S]*?\1|("|')(?:\\.|(?!\2)[^\\\r\n])*\2)/i,
			greedy: true,
			inside: {
				interpolation: {
					pattern: /((?:^|[^{])(?:{{)*){(?!{)(?:[^{}]|{(?!{)(?:[^{}]|{(?!{)(?:[^{}])+})+})+}/,
					lookbehind: true,
					inside: {
						'format-spec': {
							pattern: /(:)[^:(){}]+(?=\}$)/,
							lookbehind: true
						},
						'conversion-option': {
							pattern: /![sra](?=[:}]$)/,
							alias: 'punctuation'
						},
						rest: null
					}
				},
				string: /[\s\S]+/
			}
		},
		'triple-quoted-string': {
			pattern: /(?:[rub]|rb|br)?("""|''')[\s\S]*?\1/i,
			greedy: true,
			alias: 'string'
		},
		string: {
			pattern: /(?:[rub]|rb|br)?("|')(?:\\.|(?!\1)[^\\\r\n])*\1/i,
			greedy: true
		},
		function: {
			pattern: /((?:^|\s)def[ \t]+)[a-zA-Z_]\w*(?=\s*\()/g,
			lookbehind: true
		},
		'decorator': {
			pattern: /(^\s*)@\w+(?:\.\w+)*/im,
			lookbehind: true,
			alias: ['annotation', 'punctuation'],
			inside: { punctuation: /\./ }
		},
		keyword: /\b(?:and|as|assert|async|await|break|class|continue|def|del|elif|else|except|exec|finally|for|from|global|if|import|in|is|lambda|nonlocal|not|or|pass|print|raise|return|try|while|with|yield)\b/,
		boolean: /\b(?:True|False|None)\b/,
		number: /(?:\b(?=\d)|\B(?=\.))(?:0[bo])?(?:(?:\d|0x[\da-f])[\da-f]*\.?\d*|\.\d+)(?:e[+-]?\d+)?j?\b/i,
		operator: /[-+%=]=?|!=|\*\*?=?|\/\/?=?|<[<=>]?|>[=>]?|[&|^~]/,
		punctuation: /[{}[\];(),.:]/
	};
	_.languages.python['string-interpolation'].inside.interpolation.inside.rest = _.languages.python;
	_.languages.py = _.languages.python;

	// ============================================================
	// LANGUAGE: JSON
	// ============================================================
	_.languages.json = {
		property: {
			pattern: /"(?:\\.|[^\\"\r\n])*"(?=\s*:)/,
			greedy: true
		},
		string: {
			pattern: /"(?:\\.|[^\\"\r\n])*"(?!\s*:)/,
			greedy: true
		},
		comment: {
			pattern: /\/\/.*|\/\*[\s\S]*?(?:\*\/|$)/,
			greedy: true
		},
		number: /-?\b\d+(?:\.\d+)?(?:e[+-]?\d+)?\b/i,
		punctuation: /[{}[\],]/,
		operator: /:/,
		boolean: /\b(?:true|false)\b/,
		null: { keyword: /\bnull\b/, alias: 'keyword' }
	};

	// ============================================================
	// LANGUAGE: Bash / Shell
	// ============================================================
	_.languages.bash = {
		shebang: {
			pattern: /^#!\s*\/.*/,
			alias: 'important'
		},
		comment: {
			pattern: /(^|[\s"{\\])#.*/,
			lookbehind: true
		},
		'string': [
			{
				pattern: /((?:^|[^<])<<-?\s*)(\w+?)\s[\s\S]*?(?:\r?\n|\r)\2\b/,
				lookbehind: true,
				greedy: true,
				inside: null // set below
			},
			{
				pattern: /((?:^|[^<])<<-?\s*)(["'])(\w+)\2\s[\s\S]*?(?:\r?\n|\r)\3\b/,
				lookbehind: true,
				greedy: true
			},
			{
				pattern: /(^|[^\\](?:\\\\)*)(["'])(?:\\(?:\r\n|[\s\S])|(?!\2)[^\\\r\n]|\2\2)*\2/,
				lookbehind: true,
				greedy: true
			}
		],
		'variable': [
			{ pattern: /\$(\w+)/i },
			{
				pattern: /\$({(?:(?:[:\/?#\[\]@]|\\(?${varInside})|(?:\d|[^\w$\-])(?:(?!\${(?:$|(?![\d]))))+)+}))/,
				inside: {}
			},
			{ pattern: /\$(?:\((?:\([^()]+\)|[^()]+)+\)|<(?!#)|(?<!\$)\$\w+|#(?!\{)\w+|(?<!\$)\[\w*+\]|\{[^}]+\}|(?<!\$)\[(?:[+*\/%^]|!(?!\w))|(?<!\$)[+*\/%^]|(?<!\$)\.|[^\w\$\-])/ }
		],
		'function': {
			pattern: /(^|[\s;|&])(?:alias|apropos|apt-get|aptitude|aspell|awk|basename|bash|bc|bg|builtin|bzip2|cal|cat|cd|cfdisk|chgrp|chmod|chown|chroot|cksum|clear|cmp|comm|command|cp|cron|crontab|csplit|curl|cut|date|dc|dd|ddrescue|df|diff|diff3|dig|dir|dircolors|dirname|dirs|dmesg|du|egrep|eject|enable|env|ethtool|eval|exec|expand|expect|export|expr|fdformat|fdisk|fg|fgrep|file|find|fmt|fold|format|free|fsck|fuser|gawk|gcc|getconf|getopt|git|grep|groupadd|groupdel|groups|grub|gzip|halt|hash|head|help|hg|hostname|hp|htop|iconv|id|ifconfig|ifup|import|install|ip|jobs|join|kill|killall|less|link|ln|locate|logname|logout|look|lpc|lpr|lprint|lprintd|lprm|ls|lsof|make|man|mkdir|mkfifo|mkisofs|mknod|more|most|mount|mtools|mtr|mv|mmv|nano|netstat|nice|nl|nohup|notify-send|nslookup|open|op|passwd|paste|pathchk|ping|pkill|popd|pr|printcap|printenv|printf|ps|pushd|pwd|quota|quotacheck|quotactl|rcp|read|readlink|readonly|reboot|remsync|rename|renice|revi$|^|rev|rm|rmdir|rsync|screen|scp|sdiff|sed|sendmail|seq|service|sftp|shift|shopt|shutdown|sleep|slocate|sort|source|split|ssh|stat|strace|su|sudo|sum|suspend|swapon|sync|tac|tail|tar|tee|test|time|timeout|top|touch|tr|traceroute|trap|tr$|^|tsort|tty|type|ulimit|umask|uname|unexpand|uniq|unlink|unshift|update-grub|uptime|useradd|userdel|users|usermod|vdir|vi$|^|vim|vmstat|wait|w|wall|watch|wc|wget|whatis|which|who|whoami|write|xargs|xdg-open|yes|zip)(?=$|[\s;|&])/,
			lookbehind: true
		},
		'keyword': {
			pattern: /(^|[\s;|&])(?:let|:|\.|if|then|else|elif|fi|for|break|continue|while|in|case|function|select|do|done|until|echo|exit|return|set|declare|typeset|export|readonly)(?=$|[\s;|&])/,
			lookbehind: true
		},
		'boolean': {
			pattern: /(^|[\s;|&])(?:true|false)(?=$|[\s;|&])/,
			lookbehind: true
		},
		'operator': /&&?|\|\|?|==?|!=?|<<<?|>>|<=?|>=?|[!+\-*/%]/,
		'punctuation': /\$?\(\(?|\)\)?|\.\.|[{}[\];]/
	};
	_.languages.bash['string'][0].inside = _.languages.extend('bash', { variable: _.languages.bash.variable });
	_.languages.shell = _.languages.bash;

	// ============================================================
	// LANGUAGE: SQL
	// ============================================================
	_.languages.sql = {
		comment: {
			pattern: /(^|[^\\])(?:\/\*[\s\S]*?\*\/|(?:--|\/\/|#).*)/,
			lookbehind: true
		},
		variable: [
			{ pattern: /@(["'`])(?:\\[\s\S]|(?!\1)[^\\])+\1/, greedy: true },
			/@[\w.$]+/
		],
		string: {
			pattern: /(^|[^@\\])("|')(?:\\[\s\S]|(?!\2)[^\\]|\2\2)*\2/,
			greedy: true,
			lookbehind: true
		},
		'function': /\b(?:AVG|CHECKSUM_AGG|COUNT|COUNT_BIG|GROUPING|GROUPING_ID|MAX|MIN|STDEV|STDEVP|SUM|VAR|VARP|DENSE_RANK|NTILE|RANK|ROW_NUMBER|FIRST_VALUE|LAST_VALUE|LAG|LEAD|PERCENTILE_CONT|PERCENTILE_DISC|CAST|CONVERT|PARSE|TRY_CAST|TRY_CONVERT|TRY_PARSE|COALESCE|IIF|NULLIF|ABS|ACOS|ASIN|ATAN|ATN2|CEILING|COS|COT|DEGREES|EXP|FLOOR|LOG|LOG10|PI|POWER|RADIANS|RAND|ROUND|SIGN|SIN|SQRT|SQUARE|TAN|ASCII|CHAR|CHARINDEX|CONCAT|CONCAT_WS|DIFFERENCE|FORMAT|LEN|LOWER|LTRIM|NCHAR|PATINDEX|QUOTENAME|REPLACE|REPLICATE|REVERSE|RTRIM|SOUNDEX|SPACE|STR|STUFF|SUBSTRING|TRANSLATE|TRIM|UNICODE|UPPER|BINARY_CHECKSUM|CHECKSUM|CONNECTIONPROPERTY|CONTEXT_INFO|CURRENT_REQUEST_ID|CURRENT_TIMESTAMP|ERROR_LINE|ERROR_MESSAGE|ERROR_NUMBER|ERROR_PROCEDURE|ERROR_SEVERITY|ERROR_STATE|FORMATMESSAGE|GETANSINULL|GET_FILESTREAM_TRANSACTION_CONTEXT|HOST_ID|HOST_NAME|ISDATE|ISNULL|MIN_ACTIVE_ROWVERSION|NEWID|NEWSEQUENTIALID|ROWCOUNT_BIG|SESSIONPROPERTY|SESSION_USER|SYSTEM_USER|XACT_STATE|@@CURSOR_ROWS|@@DBTS|@@ERROR|@@IDENTITY|@@IDLE|@@IO_BUSY|@@LOCK_TIMEOUT|@@MAX_CONNECTIONS|@@NEST|@@PACK_RECEIVED|@@PACK_SENT|@@PROCID|@@ROWCOUNT|@@SERVERNAME|@@SPID|@@TEXTSIZE|@@VERSION|DATABASE_PRINCIPAL_ID|DATABASEPROPERTY|DATABASEPROPERTYEX|DB_ID|DB_NAME|FILE_ID|FILE_NAME|FILEGROUP_ID|FILEGROUP_NAME|FILEGROUPPROPERTY|FILEPROPERTY|FULLTEXTCATALOGPROPERTY|FULLTEXTSERVICEPROPERTY|INDEX_COL|INDEXKEY_PROPERTY|INDEXPROPERTY|OBJECT_DEFINITION|OBJECT_ID|OBJECT_NAME|OBJECT_SCHEMA_NAME|OBJECTPROPERTY|OBJECTPROPERTYEX|ORIGINAL_DB_NAME|SCHEMA_ID|SCHEMA_NAME|SQL_VARIANT_PROPERTY|TYPE_ID|TYPE_NAME|TYPEPROPERTY|COLLATIONPROPERTY|COLUMNPROPERTY|PROPERTYEX|CONNECTIONPROPERTY|SQL_VARIANT_PROPERTY|SYSTEM_USER|CURRENT_TIMESTAMP|DATEADD|DATEDIFF|DATEFROMPARTS|DATENAME|DATEPART|DATETIME2FROMPARTS|DATETIMEFROMPARTS|DATETIMEOFFSETFROMPARTS|DAY|EOMONTH|GETDATE|GETUTCDATE|ISDATE|MONTH|SMALLDATETIMEFROMPARTS|SMALLDATETIMEFROMPARTS|SWITCHOFFSET|SYSDATETIME|SYSDATETIMEOFFSET|SYSUTCDATETIME|TODATETIMEOFFSET|YEAR|CHOOSE|GREATEST|IIF|NULLIF|ABS|ACOS|ASIN|ATAN|ATN2|CEILING|COS|COT|DEGREES|EXP|FLOOR|LOG|LOG10|PI|POWER|RADIANS|RAND|ROUND|SIGN|SIN|SQRT|SQUARE|TAN)\b/i,
		keyword: /\b(?:ADD|ALL|ALTER|AND|ANY|AS|ASC|AUTHORIZATION|BACKUP|BEGIN|BETWEEN|BREAK|BROWSE|BULK|BY|CASCADE|CASE|CHECK|CHECKPOINT|CLOSE|CLUSTERED|COALESCE|COLLATE|COLUMN|COMMIT|COMPUTE|CONSTRAINT|CONTAINS|CONTAINSTABLE|CONTINUE|CONVERT|CREATE|CROSS|CURRENT|CURRENT_DATE|CURRENT_TIME|CURRENT_TIMESTAMP|CURRENT_USER|CURSOR|DATABASE|DBCC|DEALLOCATE|DECLARE|DEFAULT|DELETE|DENY|DESC|DISK|DISTINCT|DISTRIBUTED|DOUBLE|DROP|DUMP|ELSE|END|ERRLVL|ESCAPE|EXCEPT|EXEC|EXECUTE|EXISTS|EXIT|EXTERNAL|FETCH|FILE|FILLFACTOR|FOR|FOREIGN|FREETEXT|FREETEXTTABLE|FROM|FULL|FUNCTION|GOTO|GRANT|GROUP|HAVING|HOLDLOCK|IDENTITY|IDENTITYCOL|IDENTITY_INSERT|IF|IN|INDEX|INNER|INSERT|INTERSECT|INTO|IS|JOIN|KEY|KILL|LEFT|LIKE|LINENO|LOAD|MERGE|NATIONAL|NOCHECK|NONCLUSTERED|NOT|NULL|NULLIF|OF|OFF|OFFSETS|ON|OPEN|OPENDATASOURCE|OPENQUERY|OPENROWSET|OPENXML|OPTION|OR|ORDER|OUTER|OVER|PERCENT|PIVOT|PLAN|PRECISION|PRIMARY|PRINT|PROC|PROCEDURE|PUBLIC|RAISERROR|READ|READTEXT|RECONFIGURE|REFERENCES|REPLICATION|RESTORE|RESTRICT|RETURN|REVERT|REVOKE|RIGHT|ROLLBACK|ROWCOUNT|ROWGUIDCOL|RULE|SAVE|SCHEMA|SECURITYAUDIT|SELECT|SEMANTICKEYPHRASETABLE|SEMANTICSIMILARITYDETAILSTABLE|SEMANTICSIMILARITYTABLE|SESSION_USER|SET|SETUSER|SHUTDOWN|SOME|STATISTICS|SYSTEM_USER|TABLE|TABLESAMPLE|TEXTSIZE|THEN|TO|TOP|TRAN|TRANSACTION|TRIGGER|TRUNCATE|TRY_CONVERT|TSEQUAL|UNION|UNIQUE|UNPIVOT|UPDATE|UPDATETEXT|USE|USER|VALUES|VARYING|VIEW|WAITFOR|WHEN|WHERE|WHILE|WITH|WITHIN GROUP|WRITETEXT)\b/i,
		boolean: /\b(?:TRUE|FALSE|NULL)\b/i,
		number: /\b0x[\da-f]+\b|\b\d+\.?\d*|\B\.\d+\b/i,
		operator: /[-+*\/=%^~]|&&?|\|\|?|!=?|<(?:=>?|<|>)?|>[>=]?|\b(?:AND|BETWEEN|IN|LIKE|NOT|OR|IS|NULL|UNION|ALL|EXISTS|BETWEEN|IN|LIKE)\b/i,
		punctuation: /[;[\]()`,.]/
	};

	// ============================================================
	// LANGUAGE: Markdown
	// ============================================================
	_.languages.markdown = _.languages.extend('markup', {});
	_.languages.md = _.languages.markdown;

	// ============================================================
	// Add extend method
	// ============================================================
	_.languages.extend = function(id, redef) {
		var lang = _.util.clone(_.languages[id]);
		for (var key in redef) {
			lang[key] = redef[key];
		}
		return lang;
	};

	// ============================================================
	// Plugin: Markup (add inside for markup)
	// ============================================================
	_.languages.markup.inside['attr-value'].inside.rest = _.languages.markup;
	_.languages.markup.inside.rest = _.languages.markup;

	// ============================================================
	// Global highlight functions
	// ============================================================
	_.highlightAll = function() {
		_.highlightAllUnder(document);
	};

	_.highlightAllUnder = function(container, async, callback) {
		var env = { callback: callback, container: container, selector: 'code[class*="language-"], [class*="language-"] code, code[class*="lang-"], [class*="lang-"] code' };
		_.hooks.run('before-highlightall', env);
		env.elements = Array.prototype.slice.apply(env.container.querySelectorAll(env.selector));
		_.hooks.run('before-all-elements-highlight', env);
		for (var i = 0, element; element = env.elements[i++];) {
			_.highlightElement(element, async === true, env.callback);
		}
		_.hooks.run('after-all-elements-highlight', env);
	};

	_.highlightElement = function(element, async, callback) {
		var language = _.util.getLanguage(element);
		var grammar = _.languages[language];
		element.className = element.className.replace(lang, '').replace(/\s+/g, ' ') + ' language-' + language;
		var parent = element.parentNode;
		if (parent && parent.nodeName.toLowerCase() === 'pre') {
			parent.className = parent.className.replace(lang, '').replace(/\s+/g, ' ') + ' language-' + language;
		}
		var code = element.textContent;
		var env = { element: element, language: language, grammar: grammar, code: code };
		function insertHighlightedCode(highlightedCode) {
			env.highlightedCode = highlightedCode;
			_.hooks.run('before-insert', env);
			env.element.innerHTML = env.highlightedCode;
			_.hooks.run('after-insert', env);
			_.hooks.run('complete', env);
			callback && callback.call(env.element);
		}
		_.hooks.run('before-sanity-check', env);
		parent = env.element.parentNode;
		if (parent && parent.nodeName.toLowerCase() === 'pre' && !parent.hasAttribute('tabindex')) {
			parent.setAttribute('tabindex', '0');
		}
		if (!env.code) {
			_.hooks.run('complete', env);
			callback && callback.call(env.element);
			return;
		}
		_.hooks.run('before-highlight', env);
		if (!env.grammar) {
			insertHighlightedCode(_.util.encode(env.code));
			return;
		}
		if (async && typeof Worker !== 'undefined') {
			var worker = new Worker(_.plugins.autoloader);
			worker.onmessage = function(evt) { insertHighlightedCode(evt.data); };
			worker.postMessage(JSON.stringify({ language: env.language, code: env.code, immediateClose: true }));
		} else {
			insertHighlightedCode(_.highlight(env.code, env.grammar, env.language));
		}
	};

	return _;
})();

// Auto-highlight on DOM ready
if (typeof document !== 'undefined') {
	if (document.readyState === 'loading') {
		document.addEventListener('DOMContentLoaded', function() { Prism.highlightAll(); });
	} else {
		Prism.highlightAll();
	}
}
