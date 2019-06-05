import 'bootstrap/dist/css/bootstrap.css';
import 'bootstrap/dist/css/bootstrap-theme.css';
import 'office-ui-fabric-react/dist/css/fabric.min.css';
import './Style.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter, withRouter } from 'react-router-dom';
import App from './App';
import registerServiceWorker from './registerServiceWorker';
import { I18nextProvider } from "react-i18next";
import i18n from './i18n';
import PropTypes from 'prop-types';

const RouterContextProvider = withRouter(
    class extends React.Component {
        static childContextTypes = {
            router: PropTypes.object
        };

        displayName = "RouterContextProvider";

        getChildContext() {
            const { children, ...router } = this.props;
            return { router };
        }

        render() {
            return this.props.children;
        }
    }
);

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

ReactDOM.render(
    <I18nextProvider i18n={i18n}>
        <BrowserRouter basename={baseUrl}>
            <RouterContextProvider>
                <App i18n={i18n} />
            </RouterContextProvider>
        </BrowserRouter>
    </I18nextProvider>,
  rootElement);

registerServiceWorker();
