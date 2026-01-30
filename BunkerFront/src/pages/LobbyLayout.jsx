import { Link, Outlet } from "react-router";
import "../styles/pages/LobbyLayout.scss"

function LobbyLayout() {
    return ( 
        <>
            <div className="background lobby">
                <div className="lobby__container">
                    <Link to="/" className="lobby__back-btn">Назад</Link>  
                    <h2 className="lobby__title">Лобби</h2>
                    <Outlet />
                </div>
            </div>
        </>
    );
}

export default LobbyLayout;