import {useParams} from "react-router";

function User() {
    const { userId } = useParams<{ userId: string }>()

    return (
        <>
            <p>{userId}</p>
        </>
    )
}

export default User;