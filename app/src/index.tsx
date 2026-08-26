import React from 'react';
import ReactDOM from 'react-dom/client';
import './styles/App.css';
import App from './App';
import { Provider } from 'react-redux';
import { store } from './store/Store';
import { Analytics } from "@vercel/analytics/react"

const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);
root.render(
  <React.StrictMode>
    <Provider store={store}>
      <Analytics></Analytics>
      <App />
    </Provider>
  </React.StrictMode>
);
