import { Route, Routes } from "react-router";
import Layout from "../../pages/Layout";
import Rules from "../../pages/RulesPage";
import MainPage from "../../pages/MainPage";
import AboutUs from "../../pages/AboutUsPage";
import Login from "../../components/auth/Login";
import Registration from "../../components/auth/Registration";
import AuthLayout from "../../pages/AuthLayout";
import GamePage from "../../pages/GamePage";
import Profile from "../../pages/ProfilePage";
import LobbyLayout from "../../pages/LobbyLayout";
import Lobby from "../../components/lobby/Lobby";
import JoinRoom from "../../components/lobby/JoinRoom";
import ProtectedLayout from "../../components/auth/ProtectedLayout";
import PersistLogin from "../../components/auth/PersistLogin";
import RoomPage from "../../pages/RoomPage";
import ErrorPage from "../../pages/ErrorPage";


function appRouters() {
    return (
        <>
            <Routes>
                {/* проверка на наличие объекта user */}
                <Route element={<PersistLogin />}>

                    <Route path="/auth" element={<AuthLayout />}>
                        <Route index element={<Login />}/>
                        <Route path="registration" element={<Registration />}/>
                    </Route>


                    
                    <Route path="/" element={<Layout />}>
                        <Route path="rules" element={<Rules />}/>
                        <Route index element={<MainPage />}/>
                        <Route path="about-us" element={<AboutUs />}/>
                    </Route>


                    <Route element={<ProtectedLayout />}>
                        <Route path="/" element={<Layout />}>
                            <Route path="profile" element={<Profile />}/>
                        </Route>

                        <Route path="/lobby" element={<LobbyLayout />}>
                            <Route index element={<Lobby />}/>
                            <Route path="join" element={<JoinRoom />}/>
                        </Route>

                        <Route path="/room/:id" element={<RoomPage />} />
                        <Route path="/game/:id" element={<GamePage />} />

                    </Route>
                
                </Route> 

                <Route path="*" element={<ErrorPage />} /> 
            </Routes>
            
        </>
    );
}

export default appRouters;