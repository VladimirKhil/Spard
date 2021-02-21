const path = require('path');
const CheckerPlugin = require('awesome-typescript-loader').CheckerPlugin;

module.exports = (env) => {
	return {
		mode: 'production',
		entry: './src/SpardClient.ts',
		module: {
			rules: [
				{ test: /\.tsx?$/, use: 'awesome-typescript-loader?silent=true' }
			]
		},
		resolve: {
			extensions: ['.ts', '.js']
		},
		devtool: 'source-map',
		output: {
			path: path.resolve(__dirname, 'dist'),
			filename: 'SpardClient.js',
			library: 'SpardClient',
			libraryTarget: 'umd',
			libraryExport: 'default',
			globalObject: 'this'
		},
		externals: {
			'cross-fetch': 'cross-fetch'
		},
		optimization: {
			splitChunks: {
				chunks: 'all',
				cacheGroups: {
					commons: {
						test: /[\\/]node_modules[\\/]/,
						name: 'vendor',
						chunks: 'all'
					}
				}
			}
		},
		plugins: [
			new CheckerPlugin()
		]
	}
};
