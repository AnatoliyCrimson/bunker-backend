import { Link, Outlet } from "react-router";
import "../styles/pages/AuthLayout.scss"


function AuthLayout() { 
    return (
        <>
            <div className="background auth">
                <div className="auth__container bg-block">
                    <Outlet />
                    <div className="auth__btn-container">
                        <Link to="/" className="btn btn--small auth__btn">Назад</Link>  
                    </div>
                </div>
            </div>
        </>
    );
}

export default AuthLayout;