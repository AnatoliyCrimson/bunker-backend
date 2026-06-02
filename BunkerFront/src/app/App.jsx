import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import store from '../store';
import '../styles/index.scss'
import AppRouters from "./appRouters/AppRouters";
import ErrorBoundary from '../components/common/ErrorBoundary';
import DesktopOnlyGuard from '../components/auth/DesktopOnlyGuard';
import { SignalRProvider } from '../context/SignalRContext';

function App() {

    return (
        <>
            <Provider store={store}>
                <BrowserRouter>
                    <DesktopOnlyGuard>
                        <SignalRProvider>
                            {/* <ErrorBoundary> */}
                                <AppRouters />                    
                            {/* </ErrorBoundary> */}
                        </SignalRProvider>
                    </DesktopOnlyGuard>
                </BrowserRouter>
            </Provider>
        </>
    )
}

export default App
