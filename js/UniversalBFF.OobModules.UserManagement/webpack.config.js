// U-Shell module bundle of the UniversalBFF user management module.
// Build output follows the canonical module layout (see 'ushell-hosting'):
//   dist/module.json   <- module description (source: src/module.json)
//   dist/ui/**         <- federated widgets (remoteEntry.js + chunks)
const HtmlWebpackPlugin = require("html-webpack-plugin");
const ModuleFederationPlugin = require("webpack/lib/container/ModuleFederationPlugin");
const CopyWebpackPlugin = require("copy-webpack-plugin");
const path = require("path");
const { dependencies } = require("./package.json");

module.exports = {
  entry: "./src/index.tsx",

  output: {
    path: path.resolve(__dirname, "dist/ui"),
    // replaced by the host (UniversalBFF) with the mount url; see decision D-12 before changing
    publicPath: "_PUBLIC_PATH_",
    clean: true,
  },

  resolve: {
    extensions: [".js", ".jsx", ".ts", ".tsx"],
  },

  devServer: {
    port: 3001,
    historyApiFallback: true,
    headers: {
      "Access-Control-Allow-Origin": "*",
    },
  },

  module: {
    rules: [
      {
        test: /\.tsx?$/,
        exclude: /node_modules/,
        loader: "ts-loader",
      },
      {
        test: /\.css$/i,
        use: ["style-loader", "css-loader", "postcss-loader"],
      },
    ],
  },

  plugins: [
    new ModuleFederationPlugin({
      // unique per module - this is the 'scope' within the widgetClass of the module.json
      name: "oobUsrMgmt",
      filename: "remoteEntry.js",
      exposes: {
        "./EntityTableWidget": "./src/widgets/EntityTableWidget.tsx",
      },
      shared: {
        react: { singleton: true, requiredVersion: dependencies["react"] },
        "react-dom": { singleton: true, requiredVersion: dependencies["react-dom"] },
        "ushell-modulebase": { singleton: true, requiredVersion: dependencies["ushell-modulebase"] },
        "ushell-common-components": {
          singleton: true,
          requiredVersion: dependencies["ushell-common-components"],
          // always provided by the shell - no own fallback copy (it would be ~16 MB incl. its css)
          import: false,
        },
      },
    }),

    new HtmlWebpackPlugin({
      template: "./src/index.html",
    }),

    new CopyWebpackPlugin({
      patterns: [
        { from: "public" },
        { from: "src/module.json", to: path.resolve(__dirname, "dist/module.json") },
      ],
    }),
  ],
};
