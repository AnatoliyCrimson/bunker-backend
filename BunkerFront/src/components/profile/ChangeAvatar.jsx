import { useState } from "react";
import { useUploadAvatarMutation } from "../../store/api";
import Avatar from "../ui/Avatar";

function ChangeAvatar ({ userName, userAvatar}) {

    const [avatarError, setAvatarError] = useState('');

    const [uploadAvatar, {isLoading: avatarLoading }] = useUploadAvatarMutation();
    const handleUploadAvatar = async (e) => {
        if (avatarError) setAvatarError('');
        
        const file = e.target.files[0];
        console.log(file);
        if (!file) {
            setAvatarError("Проблема с изображением, возможно файл поврежден")
            return
        };

        if (file.size > 5 * 1024 * 1024) {
            setAvatarError("Файл слишком большой. Максимум 5 МБ")
            return;
        }

        try {
            await uploadAvatar(file).unwrap();
            e.target.value = null;
        } catch (error) {
            console.error('avatar upload failed:', error);
            setAvatarError("Не удалось загрузить аватар, ошибка сервера. Попробуйте позже")
        }
    }


    return (
        <>
            <label className={"profile__avatar-label " + (avatarLoading ? "profile__avatar-label--dis" : "")}>
                <div className='profile__avatar-edit'>
                    {avatarLoading ? (
                        <>
                            Загрузка...
                        </>
                    ) : (
                        <>
                            <svg width="80" height="81" viewBox="0 0 80 81" fill="none" xmlns="http://www.w3.org/2000/svg">
                                <path d="M1.52575 60.4379L50.3569 10.7298L69.3394 29.3774L20.5082 79.0855M1.52575 60.4379L2.2874 78.6481L20.5082 79.0855M1.52575 60.4379L20.5082 79.0855M54.1761 6.84198L73.1586 25.4896L77.7962 20.7687L58.8137 2.12109L54.1761 6.84198Z" stroke="#939393" strokeWidth="3"/>
                            </svg>                                
                            
                        </>
                    )}
                    
                </div>
                <input disabled={avatarLoading} onChange={handleUploadAvatar} type="file" accept='image/png, image/jpeg, image/jpg' />
                <Avatar
                    avatarUrl={userAvatar}
                    name={userName}
                    className={"profile__avatar-component"}
                />
            </label>
            {
                avatarError && (
                    <>
                        <p className="profile__avatar-error">
                            { avatarError } 
                        </p>
                    </>
                )
            }
            
        </>
    );
}

export default ChangeAvatar ;