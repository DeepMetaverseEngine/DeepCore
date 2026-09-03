// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface ERC20NormalInterface {

    event OwnershipTransferred(address oldManger, address newManger);

    function mint(address account, uint256 amount) external;

    function burn(address account, uint256 amount) external;

    function manager() external view returns (address);

    function transferOwnership(address newOwner) external;
}
