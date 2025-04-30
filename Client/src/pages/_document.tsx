import { Html, Head, Main, NextScript } from "next/document";

export default function Document() {
  return (
    <Html lang="en">
    <Head>
      <script src="adapter.js" />
      <script src="janus.js" />
    </Head>
      <body className="antialiased">      
        <Main />
        <NextScript />
      </body>
    </Html>
  );
}
